import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import {
  WorkflowDefinitionDetail,
  StepDefinition,
  WorkflowInstanceDetail,
  StepInstance,
  StepAction,
  FieldDefinition,
  FieldType,
  BillableUnit,
  BillableItemInput,
  SlaStatus,
  STATUS_CONFIG,
  STEP_INSTANCE_STATUS_CONFIG,
} from '../../models/workflow.models';
import { WorkflowService }       from '../../services/workflow.service';
import { StepActionModalComponent }  from '../../components/step-action-modal/step-action-modal';
import { StepAttachmentsComponent }      from '../../components/step-attachments/step-attachments';
import { InstanceAttachmentsComponent } from '../../components/instance-attachments/instance-attachments';
import { CommentThreadComponent }        from '../../components/comment-thread/comment-thread';
import { ActivityTimelineComponent }     from '../../components/activity-timeline/activity-timeline';
import { InvoiceService }            from '../../../invoicing/services/invoice.service';
import { AuthService }               from '../../../../core/auth/auth.service';
import {
  InvoiceReferenceDto,
  INVOICE_STATUS_CSS,
  INVOICE_STATUS_LABELS,
  InvoiceStatus,
  CreateWorkflowInvoiceRequest,
} from '../../../invoicing/models/invoice.models';

type DetailTab = 'steps' | 'run' | 'attachments' | 'activity' | 'history';

@Component({
  selector: 'app-workflow-detail-page',
  standalone: true,
  imports: [NgClass, DecimalPipe, RouterLink, StepActionModalComponent, StepAttachmentsComponent, InstanceAttachmentsComponent, CommentThreadComponent, ActivityTimelineComponent],
  templateUrl: './workflow-detail-page.html',
  styleUrl: './workflow-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowDetailPageComponent implements OnInit {
  private readonly route        = inject(ActivatedRoute);
  private readonly router       = inject(Router);
  private readonly wfService    = inject(WorkflowService);
  private readonly invoiceSvc   = inject(InvoiceService);
  private readonly authService  = inject(AuthService);
  private readonly destroyRef   = inject(DestroyRef);

  // ── Config exposed to template ────────────────────────────────────────────
  readonly STATUS_CONFIG               = STATUS_CONFIG;
  readonly STEP_INSTANCE_STATUS_CONFIG = STEP_INSTANCE_STATUS_CONFIG;

  // ── Definition state ──────────────────────────────────────────────────────
  readonly workflow     = signal<WorkflowDefinitionDetail | null>(null);
  readonly isLoading    = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly activeTab    = signal<DetailTab>('steps');

  /** Inline action feedback (e.g. "Activating…") */
  readonly actionPending = signal<string | null>(null);
  readonly actionError   = signal<string | null>(null);

  // ── Instance state ────────────────────────────────────────────────────────
  readonly instance       = signal<WorkflowInstanceDetail | null>(null);
  readonly isLoadingInst  = signal(false);
  readonly instanceError  = signal<string | null>(null);

  // ── Modal state ───────────────────────────────────────────────────────────
  readonly modalStep    = signal<StepInstance | null>(null);
  readonly modalAction  = signal<StepAction | null>(null);

  // ── Computed — definition ─────────────────────────────────────────────────

  readonly workflowId = computed(() => this.route.snapshot.paramMap.get('id') ?? '');

  readonly shortId = computed(() => {
    const w = this.workflow();
    return w ? w.id.substring(0, 8).toUpperCase() : '';
  });

  readonly statusCssClass = computed(() => {
    const w = this.workflow();
    return w ? (STATUS_CONFIG[w.status]?.cssClass ?? 'badge-draft') : '';
  });

  readonly statusLabel = computed(() => {
    const w = this.workflow();
    return w ? (STATUS_CONFIG[w.status]?.label ?? w.status) : '';
  });

  readonly sortedSteps = computed<StepDefinition[]>(() =>
    [...(this.workflow()?.steps ?? [])].sort((a, b) => a.order - b.order),
  );

  readonly canActivate = computed(() => this.workflow()?.status === 'Draft');
  readonly canStart    = computed(() => this.workflow()?.status === 'Active');
  readonly canDelete   = computed(() =>
    this.workflow()?.status === 'Draft' || this.workflow()?.status === 'Archived',
  );

  // ── Computed — instance ───────────────────────────────────────────────────

  readonly sortedInstanceSteps = computed<StepInstance[]>(() =>
    [...(this.instance()?.steps ?? [])].sort((a, b) => a.order - b.order),
  );

  readonly instanceTotalSteps = computed(() =>
    this.instance()?.steps.length ?? 0,
  );

  readonly instanceCompletedSteps = computed(() =>
    this.instance()?.steps.filter(s => s.status === 'Completed').length ?? 0,
  );

  readonly instanceProgress = computed(() => {
    const total = this.instanceTotalSteps();
    if (total === 0) return 0;
    return Math.round((this.instanceCompletedSteps() / total) * 100);
  });

  readonly instanceStatusClass = computed(() => {
    const inst = this.instance();
    return inst ? (STATUS_CONFIG[inst.status]?.cssClass ?? 'badge-draft') : '';
  });

  readonly instanceStatusLabel = computed(() => {
    const inst = this.instance();
    return inst ? (STATUS_CONFIG[inst.status]?.label ?? inst.status) : '';
  });

  readonly hasActiveInstance = computed(() => this.instance() !== null);

  readonly canCancel = computed(() => {
    const s = this.instance()?.status;
    return s === 'Running' || s === 'Paused';
  });

  readonly instanceBillableTotal = computed(() => this.instance()?.billableTotal ?? 0);

  /** ID of the currently authenticated user — passed to attachment components for delete permission checks. */
  readonly currentUserId = computed(() => this.authService.user()?.userId ?? '');

  /** True when the current user has the TenantAdmin role — grants manager-level delete on comments. */
  readonly isManager = computed(() =>
    this.authService.user()?.roles.includes('TenantAdmin') ?? false,
  );

  /** Total attachment count for this instance — updated by InstanceAttachmentsComponent via (countChange). */
  readonly attachmentCount = signal(0);

  // ── Invoice panel state ───────────────────────────────────────────────────

  readonly invoiceRef        = signal<InvoiceReferenceDto | null>(null);
  readonly isLoadingInvoice  = signal(false);
  readonly invoiceError      = signal<string | null>(null);
  readonly isCreatingInvoice = signal(false);

  readonly invoiceStatusCss = computed(() => {
    const ref = this.invoiceRef();
    if (!ref) return '';
    return INVOICE_STATUS_CSS[ref.status as InvoiceStatus] ?? 'inv-badge-draft';
  });

  readonly instanceIsComplete = computed(() => this.instance()?.status === 'Completed');

  /** Tracks which step IDs have their billable-items list expanded. */
  private readonly _expandedBillable    = signal(new Set<string>());
  /** Tracks which step IDs have their field-values section expanded. */
  private readonly _expandedFieldValues = signal(new Set<string>());

  readonly isExportingCsv = signal(false);

  toggleBillableStep(stepId: string): void {
    const next = new Set(this._expandedBillable());
    if (next.has(stepId)) { next.delete(stepId); } else { next.add(stepId); }
    this._expandedBillable.set(next);
  }

  isBillableExpanded(stepId: string): boolean {
    return this._expandedBillable().has(stepId);
  }

  toggleFieldValues(stepId: string): void {
    const next = new Set(this._expandedFieldValues());
    if (next.has(stepId)) { next.delete(stepId); } else { next.add(stepId); }
    this._expandedFieldValues.set(next);
  }

  isFieldValuesExpanded(stepId: string): boolean {
    return this._expandedFieldValues().has(stepId);
  }

  getFieldDef(step: StepInstance, defId: string): FieldDefinition | undefined {
    return step.fields?.find(f => f.id === defId);
  }

  /**
   * Formats a raw stored field value for display, applying type-specific
   * formatting: currency symbol, hours suffix, locale date, boolean labels.
   */
  formatFieldValue(fieldType: FieldType | undefined, value: string): string {
    switch (fieldType) {
      case 'Currency': {
        const n = parseFloat(value);
        return isNaN(n) ? value : `£${n.toFixed(2)}`;
      }
      case 'Hours':
        return `${value}h`;
      case 'Date': {
        const d = new Date(value);
        return isNaN(d.getTime())
          ? value
          : d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
      }
      case 'Boolean':
        return value === 'true' ? 'Yes' : 'No';
      default:
        return value;
    }
  }

  exportCsv(): void {
    const inst = this.instance();
    if (!inst) return;
    this.isExportingCsv.set(true);
    this.wfService.exportFieldValuesCsv(inst.id)
      .pipe(
        finalize(() => this.isExportingCsv.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a   = document.createElement('a');
          a.href     = url;
          a.download = `workflow-${inst.id.substring(0, 8)}-fields.csv`;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.actionError.set('Failed to export CSV. Please try again.'),
      });
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadWorkflow();

    // Restore instance from query param (e.g. after Start Instance)
    const instanceId = this.route.snapshot.queryParamMap.get('instance');
    if (instanceId) {
      this.loadInstance(instanceId);
      this.activeTab.set('run');
    }
  }

  // ── Definition loading ────────────────────────────────────────────────────

  loadWorkflow(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/workflows']);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.wfService
      .getDefinition(id)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  (data) => this.workflow.set(data),
        error: ()     => this.errorMessage.set('Failed to load workflow. Please try again.'),
      });
  }

  // ── Instance loading ──────────────────────────────────────────────────────

  loadInstance(instanceId: string): void {
    this.isLoadingInst.set(true);
    this.instanceError.set(null);

    this.wfService
      .getInstanceDetail(instanceId)
      .pipe(
        finalize(() => this.isLoadingInst.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (inst) => {
          this.instance.set(inst);
          if (inst.status === 'Completed') {
            this.loadInvoiceRef(instanceId);
          }
        },
        error: () => this.instanceError.set('Failed to load instance data.'),
      });
  }

  loadInvoiceRef(instanceId: string): void {
    this.isLoadingInvoice.set(true);
    this.invoiceError.set(null);

    this.invoiceSvc.getInvoiceByWorkflowInstanceId(instanceId)
      .pipe(finalize(() => this.isLoadingInvoice.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  ref  => this.invoiceRef.set(ref),
        // 404 = no invoice yet — not an error condition
        error: ()   => this.invoiceRef.set(null),
      });
  }

  createInvoice(): void {
    const inst = this.instance();
    if (!inst) return;
    this.isCreatingInvoice.set(true);
    this.invoiceError.set(null);

    // Collect explicit billable items from each step.
    // For steps that have no explicit items, fall back to any Hours / Currency
    // field values captured during step completion.
    const billableItems: BillableItemInput[] = inst.steps.flatMap(s => {
      const explicit = (s.billableItems ?? []).map(b => ({
        description: b.description,
        quantity:    b.quantity,
        unitPrice:   b.unitPrice,
        unit:        b.unit,
      }));

      if (explicit.length > 0) return explicit;

      // Fallback: derive line items from Hours / Currency field values
      return (s.fieldValues ?? []).flatMap(fv => {
        const fd = s.fields?.find(f => f.id === fv.stepFieldDefinitionId);
        if (!fd || (fd.fieldType !== 'Hours' && fd.fieldType !== 'Currency')) return [];
        const qty = parseFloat(fv.value);
        if (isNaN(qty) || qty <= 0) return [];
        return [{
          description: `${s.stepName} — ${fd.label}`,
          quantity:    fd.fieldType === 'Hours' ? qty : 1,
          unitPrice:   fd.fieldType === 'Currency' ? qty : 1,
          unit:        (fd.fieldType === 'Hours' ? 'Hours' : 'Fixed') as BillableUnit,
        }];
      });
    });

    const request: CreateWorkflowInvoiceRequest = {
      workflowName: inst.workflowName,
      clientId:     null,
      billableItems,
    };

    this.invoiceSvc.createInvoiceFromWorkflow(inst.id, request)
      .pipe(finalize(() => this.isCreatingInvoice.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.loadInvoiceRef(inst.id),
        error: () => this.invoiceError.set('Failed to create invoice draft. Please try again.'),
      });
  }

  // ── Definition actions ────────────────────────────────────────────────────

  activate(): void {
    const id = this.workflowId();
    this.actionPending.set('Activating…');
    this.actionError.set(null);

    this.wfService
      .activateDefinition(id)
      .pipe(
        finalize(() => this.actionPending.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.loadWorkflow(),
        error: () => this.actionError.set('Could not activate workflow. Please try again.'),
      });
  }

  startInstance(): void {
    const id = this.workflowId();
    this.actionPending.set('Starting…');
    this.actionError.set(null);

    this.wfService
      .startInstance(id)
      .pipe(
        finalize(() => this.actionPending.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          // Stay on this page; load the new instance and show Run tab
          this.loadInstance(result.workflowInstanceId);
          this.activeTab.set('run');
          this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { instance: result.workflowInstanceId },
            queryParamsHandling: 'merge',
          });
        },
        error: () => this.actionError.set('Could not start workflow instance. Please try again.'),
      });
  }

  cancelInstance(): void {
    const inst = this.instance();
    if (!inst) return;
    if (!confirm('Cancel this running instance? This cannot be undone.')) return;

    this.actionPending.set('Cancelling…');
    this.actionError.set(null);

    this.wfService
      .cancelInstance(inst.id)
      .pipe(
        finalize(() => this.actionPending.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.loadInstance(inst.id),
        error: () => this.actionError.set('Could not cancel instance. Please try again.'),
      });
  }

  deleteWorkflow(): void {
    const id = this.workflowId();
    if (!confirm('Delete this workflow definition? This cannot be undone.')) return;

    this.actionPending.set('Deleting…');
    this.actionError.set(null);

    this.wfService
      .deleteDefinition(id)
      .pipe(
        finalize(() => this.actionPending.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.router.navigate(['/workflows']),
        error: () => this.actionError.set('Could not delete workflow. Please try again.'),
      });
  }

  // ── Modal control ─────────────────────────────────────────────────────────

  openModal(step: StepInstance, action: StepAction): void {
    this.modalStep.set(step);
    this.modalAction.set(action);
  }

  closeModal(): void {
    this.modalStep.set(null);
    this.modalAction.set(null);
  }

  onStepActioned(): void {
    this.closeModal();
    // Reload instance to pick up fresh step statuses
    const inst = this.instance();
    if (inst) {
      this.loadInstance(inst.id);
    }
  }

  // ── Tab ───────────────────────────────────────────────────────────────────

  setTab(tab: DetailTab): void {
    this.activeTab.set(tab);
  }

  // ── Step instance helpers ─────────────────────────────────────────────────

  canAssign(step: StepInstance): boolean {
    return step.status === 'Pending' || step.status === 'InProgress';
  }

  canComplete(step: StepInstance): boolean {
    return step.status === 'InProgress' || step.status === 'Pending';
  }

  canFail(step: StepInstance): boolean {
    return step.status === 'InProgress' || step.status === 'Pending';
  }

  canSkip(step: StepInstance): boolean {
    return !step.isRequired && (step.status === 'Pending' || step.status === 'InProgress');
  }

  /** A step can be claimed if it's Pending and not yet assigned to anyone. */
  canClaim(step: StepInstance): boolean {
    return step.status === 'Pending' && !step.assigneeId;
  }

  stepInstanceStatusClass(status: string): string {
    return STEP_INSTANCE_STATUS_CONFIG[status as keyof typeof STEP_INSTANCE_STATUS_CONFIG]?.cssClass ?? 'ssi-pending';
  }

  stepInstanceStatusLabel(status: string): string {
    return STEP_INSTANCE_STATUS_CONFIG[status as keyof typeof STEP_INSTANCE_STATUS_CONFIG]?.label ?? status;
  }

  stepInstanceStatusIcon(status: string): string {
    return STEP_INSTANCE_STATUS_CONFIG[status as keyof typeof STEP_INSTANCE_STATUS_CONFIG]?.icon ?? 'pi-clock';
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day:   'numeric',
      month: 'short',
      year:  'numeric',
    });
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('en-GB', {
      day:    'numeric',
      month:  'short',
      year:   'numeric',
      hour:   '2-digit',
      minute: '2-digit',
    });
  }

  isOverdue(step: StepInstance): boolean {
    // Prefer the authoritative backend flag (set by the deadline-checker background job).
    // Fall back to client-side computation to surface imminent overdue steps
    // before the next background job tick (every 15 minutes).
    if (step.isOverdue) return true;
    if (!step.dueAt || step.status === 'Completed' || step.status === 'Skipped') return false;
    return new Date(step.dueAt) < new Date();
  }

  slaBadgeClass(status: SlaStatus | null | undefined): string {
    if (!status) return '';
    return { OnTime: 'sla-ontime', AtRisk: 'sla-atrisk', Breached: 'sla-breached' }[status] ?? '';
  }

  slaLabel(status: SlaStatus | null | undefined): string {
    if (!status) return '';
    return { OnTime: 'On Time', AtRisk: 'At Risk', Breached: 'Breached' }[status] ?? '';
  }

  requiredStepCount(steps: StepDefinition[]): number {
    return steps.filter((s) => s.isRequired).length;
  }

  trackById(_: number, s: { id: string }): string {
    return s.id;
  }

  stepDotClass(_step: StepDefinition): string {
    return 'step-dot-pending';
  }
}
