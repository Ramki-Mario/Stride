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

import { WorkflowDefinitionDetail, StepDefinition, STATUS_CONFIG, WorkflowStatus } from '../../models/workflow.models';
import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-workflow-detail-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './workflow-detail-page.html',
  styleUrl: './workflow-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowDetailPageComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly wfService  = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  // ── State ────────────────────────────────────────────────────────────────
  readonly workflow     = signal<WorkflowDefinitionDetail | null>(null);
  readonly isLoading    = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly activeTab    = signal<'steps' | 'activity' | 'history'>('steps');

  /** Inline action feedback (e.g. "Activating…") */
  readonly actionPending = signal<string | null>(null);
  readonly actionError   = signal<string | null>(null);

  // ── Computed ─────────────────────────────────────────────────────────────

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

  readonly stepProgress = computed(() => {
    const steps = this.sortedSteps();
    return steps.length;
  });

  readonly canActivate = computed(() => this.workflow()?.status === 'Draft');
  readonly canStart    = computed(() => this.workflow()?.status === 'Active');
  readonly canDelete   = computed(() =>
    this.workflow()?.status === 'Draft' || this.workflow()?.status === 'Archived',
  );

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadWorkflow();
  }

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

  // ── Actions ───────────────────────────────────────────────────────────────

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
        next: () => {
          // Refresh to pick up updated status
          this.loadWorkflow();
        },
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
          // Navigate to instance detail (Phase 4+) — for now go back to list
          this.router.navigate(['/workflows']);
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
        next: () => this.router.navigate(['/workflows']),
        error: () => this.actionError.set('Could not delete workflow. Please try again.'),
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  setTab(tab: 'steps' | 'activity' | 'history'): void {
    this.activeTab.set(tab);
  }

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

  stepDotClass(step: StepDefinition): string {
    // Definition steps have no execution state — use order-based placeholder style
    return 'step-dot-pending';
  }

  requiredStepCount(steps: StepDefinition[]): number {
    return steps.filter((s) => s.isRequired).length;
  }

  trackById(_: number, s: StepDefinition): string {
    return s.id;
  }
}
