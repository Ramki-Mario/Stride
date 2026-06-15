import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { KitOpsService } from '../../services/kit-ops.service';
import {
  KitCheckoutReportRowDto,
  KitUsageSummaryRowDto,
} from '../../models/kit-ops.models';

type Preset = 'week' | 'month' | '3months' | 'custom';
type ActiveTab = 'history' | 'summary';

@Component({
  selector: 'app-kit-reports-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="krp-page">

      <!-- ── Header ──────────────────────────────────────────────────────── -->
      <div class="krp-header">
        <div>
          <h1 class="krp-title">Kit Usage Reports</h1>
          <p class="krp-subtitle">Checkout history and per-item utilisation for your organisation.</p>
        </div>
      </div>

      <!-- ── Access denied ───────────────────────────────────────────────── -->
      @if (!isAdmin()) {
        <div class="krp-access-denied">
          <i class="pi pi-lock krp-access-icon"></i>
          <h2>Admin access required</h2>
          <p>Kit usage reports are only available to administrators.</p>
        </div>
      } @else {

        <!-- ── Tabs ──────────────────────────────────────────────────────── -->
        <div class="krp-tabs" role="tablist">
          <button class="krp-tab" role="tab"
                  [class.krp-tab--active]="activeTab() === 'history'"
                  (click)="setTab('history')">
            <i class="pi pi-list"></i> Checkout History
          </button>
          <button class="krp-tab" role="tab"
                  [class.krp-tab--active]="activeTab() === 'summary'"
                  (click)="setTab('summary')">
            <i class="pi pi-chart-bar"></i> Usage Summary
          </button>
        </div>

        <!-- ── Filter bar ────────────────────────────────────────────────── -->
        <div class="krp-filters">
          <div class="krp-presets">
            <button class="krp-preset-btn" [class.active]="preset() === 'week'"    (click)="applyPreset('week')">Last Week</button>
            <button class="krp-preset-btn" [class.active]="preset() === 'month'"   (click)="applyPreset('month')">Last Month</button>
            <button class="krp-preset-btn" [class.active]="preset() === '3months'" (click)="applyPreset('3months')">Last 3 Months</button>
            <button class="krp-preset-btn" [class.active]="preset() === 'custom'"  (click)="applyPreset('custom')">Custom</button>
          </div>

          @if (preset() === 'custom') {
            <div class="krp-date-range">
              <div class="krp-date-field">
                <label class="krp-date-label">From</label>
                <input class="krp-date-input" type="date" [(ngModel)]="customFrom" (change)="onCustomDateChange()" />
              </div>
              <span class="krp-date-sep">—</span>
              <div class="krp-date-field">
                <label class="krp-date-label">To</label>
                <input class="krp-date-input" type="date" [(ngModel)]="customTo" (change)="onCustomDateChange()" />
              </div>
            </div>
          }

          <div class="krp-filter-actions">
            <button class="btn btn-primary btn-sm" (click)="load()" [disabled]="isLoading()">
              @if (isLoading()) { <i class="pi pi-spin pi-spinner"></i> }
              @else { <i class="pi pi-search"></i> }
              Run Report
            </button>
            <button class="btn btn-outline btn-sm"
                    (click)="exportData()"
                    [disabled]="isExporting() || rows().length === 0">
              @if (isExporting()) { <i class="pi pi-spin pi-spinner"></i> }
              @else { <i class="pi pi-download"></i> }
              Export Excel
            </button>
          </div>
        </div>

        <!-- ── Date range label ──────────────────────────────────────────── -->
        @if (dateRangeLabel()) {
          <div class="krp-range-label">
            <i class="pi pi-calendar"></i> {{ dateRangeLabel() }}
            @if (rows().length > 0) {
              <span class="krp-row-count">{{ rows().length }} row{{ rows().length === 1 ? '' : 's' }}</span>
            }
          </div>
        }

        <!-- ── Error ─────────────────────────────────────────────────────── -->
        @if (error()) {
          <div class="krp-error-banner">
            <i class="pi pi-exclamation-circle"></i> {{ error() }}
          </div>
        }

        <!-- ══════════════════════════════════════════════════════════════════
             CHECKOUT HISTORY TABLE
        ══════════════════════════════════════════════════════════════════════ -->
        @if (activeTab() === 'history') {
          @if (isLoading()) {
            <div class="krp-loading"><i class="pi pi-spin pi-spinner"></i> Loading…</div>
          } @else if (historyRows().length === 0) {
            <div class="krp-empty">
              <i class="pi pi-inbox krp-empty-icon"></i>
              <p>No checkout records for the selected period.</p>
            </div>
          } @else {
            <div class="krp-table-wrap">
              <table class="krp-table">
                <thead>
                  <tr>
                    <th>Kit Item</th>
                    <th>Category</th>
                    <th>User</th>
                    <th>Checked Out</th>
                    <th>Expected Return</th>
                    <th>Returned</th>
                    <th>Status</th>
                    <th class="krp-th-right">Days Out</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of historyRows(); track row.checkoutId) {
                    <tr [class.krp-row-overdue]="row.status === 'Overdue'">
                      <td class="krp-td-name">{{ row.kitItemName }}</td>
                      <td>{{ row.category }}</td>
                      <td class="krp-td-email">{{ row.checkedOutByEmail ?? '—' }}</td>
                      <td>{{ row.checkedOutAt | date:'dd MMM yyyy' }}</td>
                      <td>{{ row.expectedReturnAt | date:'dd MMM yyyy' }}</td>
                      <td>{{ row.returnedAt ? (row.returnedAt | date:'dd MMM yyyy') : '—' }}</td>
                      <td>
                        <span class="krp-status" [class]="'krp-status--' + row.status.toLowerCase()">
                          {{ row.status }}
                        </span>
                      </td>
                      <td class="krp-td-right">{{ row.daysCheckedOut }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }

        <!-- ══════════════════════════════════════════════════════════════════
             USAGE SUMMARY TABLE
        ══════════════════════════════════════════════════════════════════════ -->
        @if (activeTab() === 'summary') {
          @if (isLoading()) {
            <div class="krp-loading"><i class="pi pi-spin pi-spinner"></i> Loading…</div>
          } @else if (summaryRows().length === 0) {
            <div class="krp-empty">
              <i class="pi pi-inbox krp-empty-icon"></i>
              <p>No usage data for the selected period.</p>
            </div>
          } @else {
            <div class="krp-table-wrap">
              <table class="krp-table">
                <thead>
                  <tr>
                    <th>Kit Item</th>
                    <th>Category</th>
                    <th class="krp-th-right">Total Qty</th>
                    <th class="krp-th-right">Total Checkouts</th>
                    <th class="krp-th-right">Active</th>
                    <th class="krp-th-right">Overdue</th>
                    <th class="krp-th-right">Pending Requests</th>
                    <th class="krp-th-right">Avg Days Out</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of summaryRows(); track row.kitItemId) {
                    <tr [class.krp-row-overdue]="row.overdueCheckouts > 0">
                      <td class="krp-td-name">{{ row.name }}</td>
                      <td>{{ row.category }}</td>
                      <td class="krp-td-right">{{ row.totalQuantity }}</td>
                      <td class="krp-td-right">{{ row.totalCheckouts }}</td>
                      <td class="krp-td-right">{{ row.activeCheckouts }}</td>
                      <td class="krp-td-right">
                        @if (row.overdueCheckouts > 0) {
                          <span class="krp-badge-danger">{{ row.overdueCheckouts }}</span>
                        } @else {
                          0
                        }
                      </td>
                      <td class="krp-td-right">{{ row.pendingReservations }}</td>
                      <td class="krp-td-right">
                        {{ row.avgDaysCheckedOut != null ? (row.avgDaysCheckedOut | number:'1.1-1') : '—' }}
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }

      } <!-- end isAdmin() -->
    </div>
  `,
  styles: [`
    .krp-page {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    /* ── Header ─── */
    .krp-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .krp-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }

    /* ── Access denied ─── */
    .krp-access-denied {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 5rem 1rem;
      gap: 0.75rem;
      color: var(--stride-text-secondary);
      text-align: center;
    }
    .krp-access-icon { font-size: 3rem; opacity: .4; }
    .krp-access-denied h2 { margin: 0; font-size: 1.125rem; color: var(--stride-text-primary); }
    .krp-access-denied p  { margin: 0; font-size: 0.875rem; }

    /* ── Tabs ─── */
    .krp-tabs {
      display: flex;
      gap: 0.25rem;
      border-bottom: 2px solid var(--stride-border);
      padding-bottom: 0;
    }
    .krp-tab {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.5rem 1rem;
      background: none;
      border: none;
      border-bottom: 2px solid transparent;
      margin-bottom: -2px;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--stride-text-secondary);
      cursor: pointer;
      transition: color 150ms, border-color 150ms;
    }
    .krp-tab:hover { color: var(--stride-text-primary); }
    .krp-tab--active {
      color: var(--stride-primary);
      border-bottom-color: var(--stride-primary);
      font-weight: 600;
    }

    /* ── Filter bar ─── */
    .krp-filters {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
    }
    .krp-presets {
      display: flex;
      gap: 0.375rem;
    }
    .krp-preset-btn {
      padding: 0.375rem 0.75rem;
      background: var(--stride-surface-card, #fff);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-md);
      font-size: 0.8125rem;
      font-weight: 500;
      color: var(--stride-text-secondary);
      cursor: pointer;
      transition: background 150ms, border-color 150ms, color 150ms;
    }
    .krp-preset-btn:hover { background: var(--stride-surface-hover, #f1f5f9); color: var(--stride-text-primary); }
    .krp-preset-btn.active {
      background: var(--stride-primary-surface, #eff6ff);
      border-color: var(--stride-primary);
      color: var(--stride-primary);
      font-weight: 600;
    }
    .krp-date-range {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .krp-date-field {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }
    .krp-date-label {
      font-size: 0.6875rem;
      color: var(--stride-text-secondary);
      font-weight: 500;
    }
    .krp-date-input {
      padding: 0.375rem 0.625rem;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-md);
      font-size: 0.8125rem;
      color: var(--stride-text-primary);
      background: var(--stride-surface-input, #fff);
    }
    .krp-date-input:focus { outline: none; border-color: var(--stride-primary); }
    .krp-date-sep { color: var(--stride-text-secondary); font-size: 0.875rem; padding-top: 1.25rem; }
    .krp-filter-actions {
      display: flex;
      gap: 0.5rem;
      margin-left: auto;
    }

    /* ── Range label ─── */
    .krp-range-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
    }
    .krp-row-count {
      background: var(--stride-surface-muted, #f1f5f9);
      padding: 0.1rem 0.5rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-text-primary);
    }

    /* ── Error ─── */
    .krp-error-banner {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.75rem 1rem;
      border-radius: var(--stride-radius-md);
      font-size: 0.875rem;
      font-weight: 500;
      background: var(--stride-danger-surface, #fef2f2);
      color: var(--stride-danger, #dc2626);
      border: 1px solid var(--stride-danger-border, #fecaca);
    }

    /* ── Loading / empty ─── */
    .krp-loading {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 2rem;
      color: var(--stride-text-secondary);
      font-size: 0.875rem;
    }
    .krp-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem 1rem;
      gap: 0.75rem;
      color: var(--stride-text-secondary);
    }
    .krp-empty-icon { font-size: 2.5rem; opacity: .4; }
    .krp-empty p { margin: 0; font-size: 0.875rem; }

    /* ── Table ─── */
    .krp-table-wrap {
      overflow-x: auto;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg, 0.75rem);
    }
    .krp-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.8125rem;
    }
    .krp-table th {
      background: var(--stride-surface-muted, #f8fafc);
      padding: 0.625rem 0.875rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.04em;
      border-bottom: 1px solid var(--stride-border);
      white-space: nowrap;
    }
    .krp-th-right { text-align: right; }
    .krp-table td {
      padding: 0.625rem 0.875rem;
      border-bottom: 1px solid var(--stride-border);
      color: var(--stride-text-primary);
      white-space: nowrap;
    }
    .krp-table tr:last-child td { border-bottom: none; }
    .krp-table tr:hover td { background: var(--stride-surface-hover, #f8fafc); }
    .krp-row-overdue td { background: var(--stride-danger-surface, #fef2f2) !important; }
    .krp-td-name  { font-weight: 600; max-width: 14rem; overflow: hidden; text-overflow: ellipsis; }
    .krp-td-email { color: var(--stride-text-secondary); max-width: 14rem; overflow: hidden; text-overflow: ellipsis; }
    .krp-td-right { text-align: right; }

    /* ── Status badge ─── */
    .krp-status {
      display: inline-block;
      padding: 0.15rem 0.5rem;
      border-radius: 999px;
      font-size: 0.6875rem;
      font-weight: 700;
    }
    .krp-status--active   { background: var(--stride-primary-surface, #eff6ff);   color: var(--stride-primary); }
    .krp-status--returned { background: var(--stride-success-surface, #f0fdf4);   color: var(--stride-success, #16a34a); }
    .krp-status--overdue  { background: var(--stride-danger-surface,  #fef2f2);   color: var(--stride-danger,  #dc2626); }

    .krp-badge-danger {
      display: inline-block;
      background: var(--stride-danger, #dc2626);
      color: #fff;
      font-size: 0.6875rem;
      font-weight: 700;
      padding: 0.1rem 0.45rem;
      border-radius: 999px;
    }

    /* ── Buttons ─── */
    .btn {
      display: inline-flex; align-items: center; gap: 0.375rem;
      padding: 0.5rem 1rem; border-radius: var(--stride-radius-md);
      font-size: 0.875rem; font-weight: 500; cursor: pointer;
      border: 1px solid transparent; transition: background 150ms, opacity 150ms;
    }
    .btn:disabled { opacity: .55; cursor: not-allowed; }
    .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.8125rem; }
    .btn-primary { background: var(--stride-primary); color: #fff; }
    .btn-primary:hover:not(:disabled) { background: var(--stride-primary-hover, #1d4ed8); }
    .btn-outline { background: transparent; color: var(--stride-primary); border-color: var(--stride-primary); }
    .btn-outline:hover:not(:disabled) { background: var(--stride-primary-surface, #eff6ff); }
  `],
})
export class KitReportsPageComponent implements OnInit {
  private readonly svc  = inject(KitOpsService);
  private readonly auth = inject(AuthService);

  readonly isAdmin = computed(() => this.auth.user()?.roles?.includes('Admin') ?? false);

  readonly activeTab = signal<ActiveTab>('history');
  readonly preset    = signal<Preset>('month');

  readonly historyRows = signal<KitCheckoutReportRowDto[]>([]);
  readonly summaryRows = signal<KitUsageSummaryRowDto[]>([]);
  readonly rows        = computed(() =>
    this.activeTab() === 'history' ? this.historyRows() : this.summaryRows());

  readonly isLoading  = signal(false);
  readonly isExporting = signal(false);
  readonly error      = signal<string | null>(null);

  // Custom date range
  customFrom = '';
  customTo   = '';

  // Resolved date range for API calls (ISO strings)
  private fromDate = signal<string | undefined>(undefined);
  private toDate   = signal<string | undefined>(undefined);

  readonly dateRangeLabel = computed(() => {
    const f = this.fromDate();
    const t = this.toDate();
    if (!f && !t) return 'All time';
    if (f && t)  return `${this.fmt(f)} – ${this.fmt(t)}`;
    if (f)       return `From ${this.fmt(f)}`;
    return `Up to ${this.fmt(t!)}`;
  });

  ngOnInit(): void {
    if (!this.isAdmin()) return;
    this.applyPreset('month');
    this.load();
  }

  setTab(tab: ActiveTab): void {
    this.activeTab.set(tab);
    this.load();
  }

  applyPreset(p: Preset): void {
    this.preset.set(p);
    if (p === 'custom') return;

    const now  = new Date();
    const days = p === 'week' ? 7 : p === 'month' ? 30 : 90;
    const from = new Date(now);
    from.setDate(from.getDate() - days);

    this.fromDate.set(this.toIso(from));
    this.toDate.set(this.toIso(now));
  }

  onCustomDateChange(): void {
    this.fromDate.set(this.customFrom ? `${this.customFrom}T00:00:00Z` : undefined);
    this.toDate.set(this.customTo   ? `${this.customTo}T23:59:59Z`   : undefined);
  }

  load(): void {
    if (!this.isAdmin()) return;
    this.isLoading.set(true);
    this.error.set(null);

    const from = this.fromDate();
    const to   = this.toDate();

    if (this.activeTab() === 'history') {
      this.svc.getCheckoutHistory(from, to).subscribe({
        next:  rows => { this.historyRows.set(rows); this.isLoading.set(false); },
        error: ()   => { this.error.set('Failed to load checkout history.'); this.isLoading.set(false); },
      });
    } else {
      this.svc.getUsageSummary(from, to).subscribe({
        next:  rows => { this.summaryRows.set(rows); this.isLoading.set(false); },
        error: ()   => { this.error.set('Failed to load usage summary.'); this.isLoading.set(false); },
      });
    }
  }

  exportData(): void {
    this.isExporting.set(true);
    const from = this.fromDate();
    const to   = this.toDate();
    const tab  = this.activeTab();
    const obs  = tab === 'history'
      ? this.svc.exportCheckoutHistory(from, to)
      : this.svc.exportUsageSummary(from, to);

    obs.subscribe({
      next: (blob) => {
        const url  = URL.createObjectURL(blob);
        const link = document.createElement('a');
        const date = new Date().toISOString().slice(0, 10);
        link.href     = url;
        link.download = tab === 'history'
          ? `kit-checkout-history-${date}.xlsx`
          : `kit-usage-summary-${date}.xlsx`;
        link.click();
        URL.revokeObjectURL(url);
        this.isExporting.set(false);
      },
      error: () => {
        this.error.set('Export failed. Please try again.');
        this.isExporting.set(false);
      },
    });
  }

  private toIso(d: Date): string {
    return d.toISOString();
  }

  private fmt(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
