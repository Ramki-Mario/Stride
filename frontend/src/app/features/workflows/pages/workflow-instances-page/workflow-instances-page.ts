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
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import {
  WorkflowInstanceSummary,
  WorkflowStatus,
  SlaStatus,
  STATUS_CONFIG,
} from '../../models/workflow.models';
import { WorkflowService } from '../../services/workflow.service';

type StatusFilter = WorkflowStatus | '';

@Component({
  selector: 'app-workflow-instances-page',
  standalone: true,
  imports: [NgClass, RouterLink],
  templateUrl: './workflow-instances-page.html',
  styleUrl: './workflow-instances-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowInstancesPageComponent implements OnInit {
  private readonly wfService   = inject(WorkflowService);
  private readonly router      = inject(Router);
  private readonly route       = inject(ActivatedRoute);
  private readonly destroyRef  = inject(DestroyRef);

  readonly STATUS_CONFIG = STATUS_CONFIG;

  // ── State ────────────────────────────────────────────────────────────────
  readonly instances    = signal<WorkflowInstanceSummary[]>([]);
  readonly isLoading    = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('');

  // ── Status tab config ────────────────────────────────────────────────────
  readonly statusTabs: { label: string; value: StatusFilter; icon: string }[] = [
    { label: 'All',       value: '',          icon: 'pi-list'         },
    { label: 'Running',   value: 'Running',   icon: 'pi-spin pi-spinner' },
    { label: 'Completed', value: 'Completed', icon: 'pi-check-circle' },
    { label: 'Failed',    value: 'Failed',    icon: 'pi-times-circle' },
    { label: 'Cancelled', value: 'Cancelled', icon: 'pi-ban'          },
  ];

  // ── Derived ──────────────────────────────────────────────────────────────
  readonly filteredInstances = computed<WorkflowInstanceSummary[]>(() => {
    const filter = this.statusFilter();
    const all    = this.instances();
    return filter ? all.filter(i => i.status === filter) : all;
  });

  readonly tabCounts = computed(() => {
    const all = this.instances();
    return {
      '':          all.length,
      Running:     all.filter(i => i.status === 'Running').length,
      Completed:   all.filter(i => i.status === 'Completed').length,
      Failed:      all.filter(i => i.status === 'Failed').length,
      Cancelled:   all.filter(i => i.status === 'Cancelled').length,
    } as Record<string, number>;
  });

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Honour ?status= query param (set by dashboard card clicks)
    const qStatus = this.route.snapshot.queryParamMap.get('status') as StatusFilter | null;
    if (qStatus) this.statusFilter.set(qStatus);

    this.loadInstances();
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  setStatus(value: StatusFilter): void {
    this.statusFilter.set(value);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: value ? { status: value } : {},
      replaceUrl: true,
    });
  }

  openInstance(inst: WorkflowInstanceSummary): void {
    this.router.navigate(
      ['/workflows', inst.workflowDefinitionId],
      { queryParams: { instance: inst.id } },
    );
  }

  // ── Data loading ─────────────────────────────────────────────────────────

  loadInstances(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.wfService
      .getInstances()
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  data  => this.instances.set(data),
        error: ()    => this.errorMessage.set('Failed to load instances. Please try again.'),
      });
  }

  // ── Display helpers ──────────────────────────────────────────────────────

  progressPct(inst: WorkflowInstanceSummary): number {
    return inst.totalSteps > 0
      ? Math.round((inst.completedSteps / inst.totalSteps) * 100)
      : 0;
  }

  statusClass(status: WorkflowStatus): string {
    return STATUS_CONFIG[status]?.cssClass ?? 'badge-draft';
  }

  statusLabel(status: WorkflowStatus): string {
    return STATUS_CONFIG[status]?.label ?? status;
  }

  relativeDate(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const days = Math.floor(diff / 86_400_000);
    const hrs  = Math.floor(diff / 3_600_000);
    const mins = Math.floor(diff / 60_000);
    if (mins < 1)  return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    if (hrs  < 24) return `${hrs}h ago`;
    if (days === 1) return 'Yesterday';
    if (days < 30)  return `${days}d ago`;
    return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  slaBadgeClass(status: SlaStatus | null | undefined): string {
    if (!status) return '';
    return { OnTime: 'wi-sla-ontime', AtRisk: 'wi-sla-atrisk', Breached: 'wi-sla-breached' }[status] ?? '';
  }

  slaLabel(status: SlaStatus | null | undefined): string {
    if (!status) return '';
    return { OnTime: 'On Time', AtRisk: 'At Risk', Breached: 'Breached' }[status] ?? '';
  }

  trackById(_: number, inst: WorkflowInstanceSummary): string {
    return inst.id;
  }
}
