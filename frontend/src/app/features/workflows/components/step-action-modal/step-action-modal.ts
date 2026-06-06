import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { StepInstance, StepAction } from '../../models/workflow.models';
import { WorkflowService } from '../../services/workflow.service';

/**
 * StepActionModalComponent
 *
 * Self-contained overlay modal for all four step actions:
 *   - assign   → POST /bff/workflows/instances/:iid/steps/:sid/assign
 *   - complete → POST /bff/workflows/instances/:iid/steps/:sid/complete
 *   - fail     → POST /bff/workflows/instances/:iid/steps/:sid/fail
 *   - skip     → POST /bff/workflows/instances/:iid/steps/:sid/skip
 *
 * Usage:
 *   <app-step-action-modal
 *     [instanceId]="instanceId"
 *     [step]="selectedStep"
 *     [action]="modalAction"
 *     (dismissed)="closeModal()"
 *     (actionCompleted)="onStepActioned()"
 *   />
 *
 * The host is responsible for showing/hiding the component (e.g. @if (modalAction())).
 */
@Component({
  selector: 'app-step-action-modal',
  standalone: true,
  imports: [NgClass, ReactiveFormsModule],
  templateUrl: './step-action-modal.html',
  styleUrl: './step-action-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepActionModalComponent implements OnChanges {
  private readonly fb         = inject(FormBuilder);
  private readonly wfService  = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  // ── Inputs ────────────────────────────────────────────────────────────────

  @Input({ required: true }) instanceId!: string;
  @Input({ required: true }) step!: StepInstance;
  @Input({ required: true }) action!: StepAction;

  // ── Outputs ───────────────────────────────────────────────────────────────

  @Output() dismissed      = new EventEmitter<void>();
  @Output() actionCompleted = new EventEmitter<void>();

  // ── State ─────────────────────────────────────────────────────────────────

  readonly isSubmitting = signal(false);
  readonly submitError  = signal<string | null>(null);

  // ── Forms ─────────────────────────────────────────────────────────────────

  /** Used for Assign action — requires assignee ID. */
  readonly assignForm: FormGroup = this.fb.group({
    assigneeId: ['', [Validators.required, Validators.pattern(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    )]],
  });

  /** Used for Fail action — requires a reason. */
  readonly failForm: FormGroup = this.fb.group({
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnChanges(): void {
    // Reset form state whenever the modal is re-opened for a new step/action
    this.assignForm.reset();
    this.failForm.reset();
    this.submitError.set(null);
  }

  // ── Metadata helpers ──────────────────────────────────────────────────────

  get title(): string {
    switch (this.action) {
      case 'assign':   return 'Assign Step';
      case 'complete': return 'Complete Step';
      case 'fail':     return 'Fail Step';
      case 'skip':     return 'Skip Step';
      default:         return '';
    }
  }

  get iconClass(): string {
    switch (this.action) {
      case 'assign':   return 'pi pi-user-plus  modal-icon-primary';
      case 'complete': return 'pi pi-check-circle modal-icon-success';
      case 'fail':     return 'pi pi-times-circle modal-icon-error';
      case 'skip':     return 'pi pi-forward      modal-icon-warning';
      default:         return '';
    }
  }

  get submitLabel(): string {
    switch (this.action) {
      case 'assign':   return 'Assign';
      case 'complete': return 'Mark Complete';
      case 'fail':     return 'Mark Failed';
      case 'skip':     return 'Skip Step';
      default:         return '';
    }
  }

  get submitClass(): string {
    switch (this.action) {
      case 'assign':   return 'sam-btn-primary';
      case 'complete': return 'sam-btn-success';
      case 'fail':     return 'sam-btn-danger';
      case 'skip':     return 'sam-btn-warning';
      default:         return '';
    }
  }

  // ── Field validation helpers ──────────────────────────────────────────────

  isAssigneeInvalid(): boolean {
    const c = this.assignForm.get('assigneeId');
    return !!(c?.invalid && c.touched);
  }

  isReasonInvalid(): boolean {
    const c = this.failForm.get('reason');
    return !!(c?.invalid && c.touched);
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.submitError.set(null);

    switch (this.action) {
      case 'assign':   this.submitAssign();   break;
      case 'complete': this.submitComplete(); break;
      case 'fail':     this.submitFail();     break;
      case 'skip':     this.submitSkip();     break;
    }
  }

  private submitAssign(): void {
    this.assignForm.markAllAsTouched();
    if (this.assignForm.invalid) return;

    const { assigneeId } = this.assignForm.getRawValue() as { assigneeId: string };
    this.dispatch(this.wfService.assignStep(this.instanceId, this.step.id, assigneeId));
  }

  private submitComplete(): void {
    this.dispatch(this.wfService.completeStep(this.instanceId, this.step.id));
  }

  private submitFail(): void {
    this.failForm.markAllAsTouched();
    if (this.failForm.invalid) return;

    const { reason } = this.failForm.getRawValue() as { reason: string };
    this.dispatch(this.wfService.failStep(this.instanceId, this.step.id, reason));
  }

  private submitSkip(): void {
    this.dispatch(this.wfService.skipStep(this.instanceId, this.step.id));
  }

  private dispatch(obs$: ReturnType<typeof this.wfService.completeStep>): void {
    this.isSubmitting.set(true);
    obs$
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.actionCompleted.emit(),
        error: (err) => this.submitError.set(this.mapError(err)),
      });
  }

  dismiss(): void {
    this.dismissed.emit();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private mapError(err: { status?: number; error?: { error?: string } }): string {
    if (err?.status === 404) return 'Step or instance not found.';
    if (err?.status === 400) return err?.error?.error ?? 'Invalid operation on this step.';
    return 'An unexpected error occurred. Please try again.';
  }
}
