import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass, DecimalPipe } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import {
  StepInstance,
  StepAction,
  BillableUnit,
  BILLABLE_UNIT_LABELS,
  BillableItemInput,
  FieldDefinition,
  FieldValueInput,
} from '../../models/workflow.models';
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
  imports: [NgClass, DecimalPipe, ReactiveFormsModule],
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

  /** Used for Complete action — optional billable items list. */
  readonly billableItemsArray = this.fb.array<FormGroup>([]);
  readonly fieldValuesArray = this.fb.array<FormGroup>([]);

  readonly billableUnits: BillableUnit[] = ['Hours', 'Each', 'Day', 'Fixed'];
  readonly billableUnitLabels = BILLABLE_UNIT_LABELS;

  /** Reactive running total — subscribes to form value changes. */
  readonly billableRunningTotal = signal(0);

  get billableControls(): FormGroup[] {
    return this.billableItemsArray.controls as FormGroup[];
  }

  get fieldValueControls(): FormGroup[] {
    return this.fieldValuesArray.controls as FormGroup[];
  }

  get orderedFields(): FieldDefinition[] {
    return [...(this.step?.fields ?? [])]
      .sort((a, b) => a.displayOrder - b.displayOrder);
  }

  addBillableItem(): void {
    this.billableItemsArray.push(this.fb.group({
      description: ['', [Validators.required, Validators.maxLength(500)]],
      quantity:    [1,  [Validators.required, Validators.min(0.0001)]],
      unitPrice:   [0,  [Validators.required, Validators.min(0.0001)]],
      unit:        ['Hours' as BillableUnit],
    }));
    this._recalcTotal();
  }

  removeBillableItem(index: number): void {
    this.billableItemsArray.removeAt(index);
    this._recalcTotal();
  }

  onBillableChange(): void { this._recalcTotal(); }

  private _recalcTotal(): void {
    const total = this.billableItemsArray.controls.reduce((sum, ctrl) => {
      const qty   = Number(ctrl.get('quantity')?.value)  || 0;
      const price = Number(ctrl.get('unitPrice')?.value) || 0;
      return sum + qty * price;
    }, 0);
    this.billableRunningTotal.set(total);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnChanges(): void {
    // Reset form state whenever the modal is re-opened for a new step/action
    this.assignForm.reset();
    this.failForm.reset();
    this.billableItemsArray.clear();
    this.fieldValuesArray.clear();
    this.buildFieldValueControls();
    this.billableRunningTotal.set(0);
    this.submitError.set(null);
  }

  private buildFieldValueControls(): void {
    for (const field of this.orderedFields) {
      this.fieldValuesArray.push(this.fb.group({
        stepFieldDefinitionId: [field.id],
        value: [this.defaultFieldValue(field), this.fieldValidators(field)],
      }));
    }
  }

  private defaultFieldValue(field: FieldDefinition): string | boolean {
    return field.fieldType === 'Boolean' ? false : '';
  }

  private fieldValidators(field: FieldDefinition) {
    return field.isRequired && field.fieldType !== 'Boolean'
      ? [Validators.required]
      : [];
  }

  // ── Metadata helpers ──────────────────────────────────────────────────────

  get title(): string {
    switch (this.action) {
      case 'assign':   return 'Assign Step';
      case 'claim':    return 'Claim Step';
      case 'complete': return 'Complete Step';
      case 'fail':     return 'Fail Step';
      case 'skip':     return 'Skip Step';
      default:         return '';
    }
  }

  get iconClass(): string {
    switch (this.action) {
      case 'assign':   return 'pi pi-user-plus  modal-icon-primary';
      case 'claim':    return 'pi pi-hand-paper  modal-icon-primary';
      case 'complete': return 'pi pi-check-circle modal-icon-success';
      case 'fail':     return 'pi pi-times-circle modal-icon-error';
      case 'skip':     return 'pi pi-forward      modal-icon-warning';
      default:         return '';
    }
  }

  get submitLabel(): string {
    switch (this.action) {
      case 'assign':   return 'Assign';
      case 'claim':    return 'Claim Step';
      case 'complete': return 'Mark Complete';
      case 'fail':     return 'Mark Failed';
      case 'skip':     return 'Skip Step';
      default:         return '';
    }
  }

  get submitClass(): string {
    switch (this.action) {
      case 'assign':   return 'sam-btn-primary';
      case 'claim':    return 'sam-btn-primary';
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

  isFieldInvalid(index: number): boolean {
    const c = this.fieldValuesArray.at(index)?.get('value');
    return !!(c?.invalid && c.touched);
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  onSubmit(): void {
    this.submitError.set(null);

    switch (this.action) {
      case 'assign':   this.submitAssign();   break;
      case 'claim':    this.submitClaim();    break;
      case 'complete': this.submitComplete(); break;
      case 'fail':     this.submitFail();     break;
      case 'skip':     this.submitSkip();     break;
    }
  }

  private submitClaim(): void {
    this.dispatch(this.wfService.claimStep(this.instanceId, this.step.id));
  }

  private submitAssign(): void {
    this.assignForm.markAllAsTouched();
    if (this.assignForm.invalid) return;

    const { assigneeId } = this.assignForm.getRawValue() as { assigneeId: string };
    this.dispatch(this.wfService.assignStep(this.instanceId, this.step.id, assigneeId));
  }

  private submitComplete(): void {
    // Validate all billable item rows before submitting
    this.billableItemsArray.controls.forEach(c => c.markAllAsTouched());
    this.fieldValuesArray.controls.forEach(c => c.markAllAsTouched());
    if (this.billableItemsArray.invalid || this.fieldValuesArray.invalid) return;

    const items: BillableItemInput[] = this.billableItemsArray.controls.map(ctrl => ({
      description: ctrl.get('description')!.value as string,
      quantity:    Number(ctrl.get('quantity')!.value),
      unitPrice:   Number(ctrl.get('unitPrice')!.value),
      unit:        ctrl.get('unit')!.value as BillableUnit,
    }));

    const fieldValues = this.buildFieldValuePayload();

    this.dispatch(this.wfService.completeStep(
      this.instanceId,
      this.step.id,
      items.length ? items : undefined,
      fieldValues.length ? fieldValues : undefined,
    ));
  }

  private buildFieldValuePayload(): FieldValueInput[] {
    return this.fieldValuesArray.controls
      .map((ctrl, index) => {
        const field = this.orderedFields[index];
        const raw = ctrl.get('value')!.value;
        const value = field.fieldType === 'Boolean'
          ? String(Boolean(raw))
          : String(raw ?? '').trim();

        return {
          field,
          input: {
            stepFieldDefinitionId: ctrl.get('stepFieldDefinitionId')!.value as string,
            value,
          },
        };
      })
      .filter(({ field, input }) =>
        field.fieldType === 'Boolean'
          ? field.isRequired || input.value === 'true'
          : field.isRequired || input.value.length > 0)
      .map(({ input }) => input);
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
