import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Select } from 'primeng/select';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  WorkflowDefinitionSummary,
  WorkflowStatus,
  STATUS_CONFIG,
  SortKey,
} from '../../models/workflow.models';
import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-workflows-page',
  standalone: true,
  imports: [NgClass, FormsModule, Select],
  templateUrl: './workflows-page.html',
  styleUrl: './workflows-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowsPageComponent implements OnInit {
  private readonly wfService  = inject(WorkflowService);
  private readonly router     = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // ── Expose helpers to template ──────────────────────────────────────────
  readonly STATUS_CONFIG = STATUS_CONFIG;

  readonly statusOptions = [
    { label: 'All Statuses', value: '' },
    { label: 'Draft',        value: 'Draft' },
    { label: 'Active',       value: 'Active' },
    { label: 'Archived',     value: 'Archived' },
  ];

  readonly sortOptions = [
    { label: 'Last Updated', value: 'updatedAt' },
    { label: 'Name A–Z',     value: 'name' },
    { label: 'Created',      value: 'createdAt' },
    { label: 'Status',       value: 'status' },
    { label: 'Step Count',   value: 'stepCount' },
  ];
  readonly PAGE_SIZE = 10;

  // ── Server state ────────────────────────────────────────────────────────
  readonly workflows    = signal<WorkflowDefinitionSummary[]>([]);
  readonly isLoading    = signal(true);
  readonly errorMessage = signal<string | null>(null);

  // ── Filter / sort / view state ──────────────────────────────────────────
  readonly searchTerm   = signal('');
  readonly statusFilter = signal<WorkflowStatus | ''>('');
  readonly sortBy       = signal<SortKey>('updatedAt');
  readonly viewMode     = signal<'table' | 'cards'>('table');
  readonly currentPage  = signal(1);

  // ── Selection state ─────────────────────────────────────────────────────
  readonly selectedIds  = signal<Set<string>>(new Set());

  // ── Derived ─────────────────────────────────────────────────────────────

  readonly filteredWorkflows = computed<WorkflowDefinitionSummary[]>(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const status = this.statusFilter();
    const sort   = this.sortBy();

    let data = this.workflows();

    if (search) {
      data = data.filter(
        (w) =>
          w.name.toLowerCase().includes(search) ||
          w.id.toLowerCase().includes(search) ||
          (w.description ?? '').toLowerCase().includes(search),
      );
    }

    if (status) {
      data = data.filter((w) => w.status === status);
    }

    return [...data].sort((a, b) => {
      switch (sort) {
        case 'name':      return a.name.localeCompare(b.name);
        case 'status':    return a.status.localeCompare(b.status);
        case 'stepCount': return b.stepCount - a.stepCount;
        case 'createdAt': return b.createdAt.localeCompare(a.createdAt);
        default:          return b.updatedAt.localeCompare(a.updatedAt); // newest first
      }
    });
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredWorkflows().length / this.PAGE_SIZE)),
  );

  readonly paginatedWorkflows = computed<WorkflowDefinitionSummary[]>(() => {
    const page  = this.currentPage();
    const start = (page - 1) * this.PAGE_SIZE;
    return this.filteredWorkflows().slice(start, start + this.PAGE_SIZE);
  });

  readonly statusCounts = computed(() => {
    const all = this.workflows();
    return {
      all:      all.length,
      draft:    all.filter((w) => w.status === 'Draft').length,
      active:   all.filter((w) => w.status === 'Active').length,
      archived: all.filter((w) => w.status === 'Archived').length,
    };
  });

  readonly selectionCount = computed(() => this.selectedIds().size);

  readonly pagesArray = computed(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1),
  );

  readonly showingFrom = computed(
    () => (this.currentPage() - 1) * this.PAGE_SIZE + 1,
  );

  readonly showingTo = computed(() =>
    Math.min(this.currentPage() * this.PAGE_SIZE, this.filteredWorkflows().length),
  );

  // Reset page when filters change
  private readonly _resetPageOnFilter = effect(() => {
    this.searchTerm();
    this.statusFilter();
    this.sortBy();
    this.currentPage.set(1);
  });

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadWorkflows();
  }

  loadWorkflows(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.wfService
      .getDefinitions()
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  (data) => this.workflows.set(data),
        error: ()     => this.errorMessage.set('Failed to load workflows. Please try again.'),
      });
  }

  // ── Filter actions ───────────────────────────────────────────────────────

  setStatusFilter(status: WorkflowStatus | ''): void {
    this.statusFilter.set(status);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.statusFilter.set('');
    this.sortBy.set('updatedAt');
  }

  setView(mode: 'table' | 'cards'): void {
    this.viewMode.set(mode);
  }

  // ── Selection ────────────────────────────────────────────────────────────

  toggleRow(id: string): void {
    this.selectedIds.update((set) => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggleAll(checked: boolean): void {
    if (checked) {
      const ids = new Set(this.paginatedWorkflows().map((w) => w.id));
      this.selectedIds.set(ids);
    } else {
      this.selectedIds.set(new Set());
    }
  }

  get allOnPageSelected(): boolean {
    const page = this.paginatedWorkflows();
    return page.length > 0 && page.every((w) => this.selectedIds().has(w.id));
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  // ── Bulk actions ─────────────────────────────────────────────────────────

  bulkDelete(): void {
    const ids = [...this.selectedIds()];
    if (!ids.length) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin(ids.map(id => this.wfService.deleteDefinition(id))).subscribe({
      next: () => {
        this.clearSelection();
        this.loadWorkflows();
      },
      error: () => {
        this.errorMessage.set('Failed to delete one or more workflows. Please try again.');
        this.isLoading.set(false);
      },
    });
  }

  // ── Navigation ───────────────────────────────────────────────────────────

  openWorkflow(id: string): void {
    this.router.navigate(['/workflows', id]);
  }

  navigateToCreate(): void {
    this.router.navigate(['/workflows', 'new']);
  }

  // ── Pagination ───────────────────────────────────────────────────────────

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  // ── Display helpers ──────────────────────────────────────────────────────

  statusClass(status: WorkflowStatus): string {
    return STATUS_CONFIG[status]?.cssClass ?? 'badge-draft';
  }

  statusLabel(status: WorkflowStatus): string {
    return STATUS_CONFIG[status]?.label ?? status;
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day:   'numeric',
      month: 'short',
      year:  'numeric',
    });
  }

  relativeDate(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const days = Math.floor(diff / 86_400_000);
    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    if (days < 30)  return `${days}d ago`;
    if (days < 365) return `${Math.floor(days / 30)}mo ago`;
    return `${Math.floor(days / 365)}y ago`;
  }

  progressPct(_w: WorkflowDefinitionSummary): number {
    return 0; // definition progress (n/a — shows step count only)
  }

  trackById(_: number, w: WorkflowDefinitionSummary): string {
    return w.id;
  }
}
