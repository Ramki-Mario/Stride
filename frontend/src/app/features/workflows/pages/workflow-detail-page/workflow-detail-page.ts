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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import {
  WorkflowDefinitionDetail,
  StepDefinition,
  WorkflowInstanceDetail,
  StepInstance,
  StepAction,
  STATUS_CONFIG,
  STEP_INSTANCE_STATUS_CONFIG,
  WorkflowStatus,
} from '../../models/workflow.models';
import { WorkflowService }       from '../../services/workflow.service';
import { StepActionModalComponent } from '../../components/step-action-modal/step-action-modal';

type DetailTab = 'steps' | 'run' | 'activity' | 'history';

@Component({
  selector: 'app-workflow-detail-page',
  standalone: true,
  imports: [NgClass, RouterLink, StepActionModalComponent],
  templateUrl: './workflow-detail-page.html',
  styleUrl: './workflow-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowDetailPageComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly wfService  = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

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

  readonly instanceProgress = computed(() => {
    const inst = this.instance();
    if (!inst || inst.totalSteps === 0) return 0;
    return Math.round((inst.completedSteps / inst.totalSteps) * 100);
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
        next:  (inst) => this.instance.set(inst),
        error: ()     => this.instanceError.set('Failed to load instance data.'),
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
