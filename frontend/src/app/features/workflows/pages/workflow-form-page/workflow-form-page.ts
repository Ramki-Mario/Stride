import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-workflow-form-page',
  standalone: true,
  imports: [NgClass, ReactiveFormsModule, RouterLink],
  templateUrl: './workflow-form-page.html',
  styleUrl: './workflow-form-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowFormPageComponent implements OnInit {
  private readonly fb         = inject(FormBuilder);
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly wfService  = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Mode ──────────────────────────────────────────────────────────────────

  readonly isEditMode  = signal(false);
  readonly workflowId  = signal<string | null>(null);

  // ── Async state ───────────────────────────────────────────────────────────

  readonly isLoadingWorkflow = signal(false);
  readonly isSaving          = signal(false);
  readonly loadError         = signal<string | null>(null);
  readonly saveError         = signal<string | null>(null);

  // ── Computed titles ───────────────────────────────────────────────────────

  readonly pageTitle   = computed(() => this.isEditMode() ? 'Edit Workflow'   : 'New Workflow');
  readonly submitLabel = computed(() => this.isEditMode() ? 'Save Changes'    : 'Create Workflow');
  readonly breadcrumb  = computed(() => this.isEditMode() ? 'Edit'            : 'New');

  // ── Form ──────────────────────────────────────────────────────────────────

  readonly form: FormGroup = this.fb.group({
    name:        ['', [Validators.required, Validators.maxLength(100)]],
    description: [''],
  });

  readonly stepsArray: FormArray = this.fb.array([] as AbstractControl[]);

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.workflowId.set(id);
      this.loadForEdit(id);
    } else {
      // Create mode — start with one empty step
      this.addStep();
    }
  }

  private loadForEdit(id: string): void {
    this.isLoadingWorkflow.set(true);
    this.wfService
      .getDefinition(id)
      .pipe(
        finalize(() => this.isLoadingWorkflow.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (wf) => {
          if (wf.status !== 'Draft') {
            // Only Draft workflows can be edited — redirect to detail
            this.router.navigate(['/workflows', id]);
            return;
          }
          this.form.patchValue({
            name:        wf.name,
            description: wf.description ?? '',
          });
        },
        error: () => this.loadError.set('Failed to load workflow. Please try again.'),
      });
  }

  // ── Steps management ──────────────────────────────────────────────────────

  get steps(): FormGroup[] {
    return this.stepsArray.controls as FormGroup[];
  }

  addStep(): void {
    const stepGroup = this.fb.group({
      name:        ['', [Validators.required, Validators.maxLength(100)]],
      description: [''],
      isRequired:  [true],
    });
    this.stepsArray.push(stepGroup);
  }

  removeStep(index: number): void {
    if (this.stepsArray.length > 1) {
      this.stepsArray.removeAt(index);
    }
  }

  moveUp(index: number): void {
    if (index === 0) return;
    const ctrl = this.stepsArray.at(index);
    this.stepsArray.removeAt(index);
    this.stepsArray.insert(index - 1, ctrl);
  }

  moveDown(index: number): void {
    if (index >= this.stepsArray.length - 1) return;
    const ctrl = this.stepsArray.at(index);
    this.stepsArray.removeAt(index);
    this.stepsArray.insert(index + 1, ctrl);
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (!this.isEditMode()) {
      this.stepsArray.markAllAsTouched();
    }

    if (this.form.invalid) return;
    if (!this.isEditMode() && this.stepsArray.invalid) return;

    const { name, description } = this.form.getRawValue();
    const descValue = (description as string)?.trim() || null;

    this.isSaving.set(true);
    this.saveError.set(null);

    if (this.isEditMode()) {
      this.wfService
        .updateDefinition(this.workflowId()!, { name, description: descValue })
        .pipe(
          finalize(() => this.isSaving.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next:  () => this.router.navigate(['/workflows', this.workflowId()]),
          error: (err) => this.saveError.set(this.mapError(err)),
        });
    } else {
      const steps = (this.stepsArray.value as Array<{ name: string; description: string; isRequired: boolean }>).map(
        (s) => ({
          name:        s.name,
          description: s.description?.trim() || null,
          isRequired:  s.isRequired,
        }),
      );

      this.wfService
        .createDefinition({ name, description: descValue, steps })
        .pipe(
          finalize(() => this.isSaving.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next:  (res) => this.router.navigate(['/workflows', res.workflowDefinitionId]),
          error: (err) => this.saveError.set(this.mapError(err)),
        });
    }
  }

  cancel(): void {
    const id = this.workflowId();
    this.router.navigate(id ? ['/workflows', id] : ['/workflows']);
  }

  // ── Validation helpers ────────────────────────────────────────────────────

  isInvalid(controlName: string): boolean {
    const c = this.form.get(controlName);
    return !!(c?.invalid && c.touched);
  }

  isStepInvalid(stepIndex: number, field: string): boolean {
    const c = this.stepsArray.at(stepIndex)?.get(field);
    return !!(c?.invalid && c.touched);
  }

  requiredError(controlName: string): boolean {
    const c = this.form.get(controlName);
    return !!(c?.touched && c.hasError('required'));
  }

  maxLengthError(controlName: string): boolean {
    const c = this.form.get(controlName);
    return !!(c?.touched && c.hasError('maxlength'));
  }

  stepRequiredError(stepIndex: number, field: string): boolean {
    const c = this.stepsArray.at(stepIndex)?.get(field);
    return !!(c?.touched && c.hasError('required'));
  }

  stepMaxLengthError(stepIndex: number, field: string): boolean {
    const c = this.stepsArray.at(stepIndex)?.get(field);
    return !!(c?.touched && c.hasError('maxlength'));
  }

  // ── Misc ──────────────────────────────────────────────────────────────────

  trackByIndex(index: number): number {
    return index;
  }

  private mapError(err: { status?: number; error?: { detail?: string } }): string {
    if (err?.status === 409) return 'A workflow with this name already exists in your tenant.';
    if (err?.status === 400) return err?.error?.detail ?? 'Validation error — please check your inputs.';
    return 'An unexpected error occurred. Please try again.';
  }
}
