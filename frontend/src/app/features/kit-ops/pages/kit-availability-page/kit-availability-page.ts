import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { KitOpsService } from '../../services/kit-ops.service';
import { KitCatalogItemDto } from '../../models/kit-ops.models';

const REFRESH_INTERVAL_MS = 30_000;

@Component({
  selector: 'app-kit-availability-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="kap-page">

      <!-- ── Header ──────────────────────────────────────────────────────── -->
      <div class="kap-header">
        <div>
          <h1 class="kap-title">Kit Availability</h1>
          <p class="kap-subtitle">Live view of all equipment — updates automatically every 30 seconds.</p>
        </div>
        <div class="kap-header-actions">
          @if (lastUpdated()) {
            <span class="kap-last-updated">
              Updated {{ lastUpdated() | date:'HH:mm:ss' }}
            </span>
          }
          <button class="btn btn-secondary btn-sm"
                  (click)="refresh()"
                  [disabled]="isLoading()">
            <i class="pi" [class.pi-refresh]="!isLoading()" [class.pi-spin]="isLoading()" [class.pi-spinner]="isLoading()"></i>
            Refresh
          </button>
        </div>
      </div>

      <!-- ── Error ───────────────────────────────────────────────────────── -->
      @if (error()) {
        <div class="kap-error-banner">
          <i class="pi pi-exclamation-circle"></i>
          <span>{{ error() }}</span>
        </div>
      }

      <!-- ── KPI cards ────────────────────────────────────────────────────── -->
      <div class="kap-kpis">
        <div class="kap-kpi">
          <div class="kap-kpi-value">{{ kpiTotalItems() }}</div>
          <div class="kap-kpi-label">Total Items</div>
        </div>
        <div class="kap-kpi kap-kpi--good">
          <div class="kap-kpi-value">{{ kpiAvailableUnits() }}</div>
          <div class="kap-kpi-label">Available Units</div>
        </div>
        <div class="kap-kpi kap-kpi--warn">
          <div class="kap-kpi-value">{{ kpiOutUnits() }}</div>
          <div class="kap-kpi-label">Checked Out</div>
        </div>
        <div class="kap-kpi" [class.kap-kpi--danger]="kpiOverdue() > 0">
          <div class="kap-kpi-value">{{ kpiOverdue() }}</div>
          <div class="kap-kpi-label">Overdue</div>
        </div>
      </div>

      <!-- ── Filter ────────────────────────────────────────────────────────── -->
      <div class="kap-filter-bar">
        <div class="kap-search-wrap">
          <i class="pi pi-search kap-search-icon"></i>
          <input class="kap-search"
                 type="text"
                 [(ngModel)]="filterText"
                 placeholder="Filter by name or category…"
                 aria-label="Filter kit items" />
          @if (filterText) {
            <button class="kap-search-clear" (click)="filterText = ''" aria-label="Clear filter">
              <i class="pi pi-times"></i>
            </button>
          }
        </div>
        <div class="kap-legend">
          <span class="kap-dot kap-dot--available"></span> Available
          <span class="kap-dot kap-dot--low"></span> Low
          <span class="kap-dot kap-dot--out"></span> All out
        </div>
      </div>

      <!-- ── Skeleton ────────────────────────────────────────────────────── -->
      @if (isLoading() && catalog().length === 0) {
        <div class="kap-grid">
          @for (sk of [1,2,3,4,5,6]; track sk) {
            <div class="kap-card kap-skeleton"></div>
          }
        </div>
      }

      <!-- ── Empty ───────────────────────────────────────────────────────── -->
      @else if (!isLoading() && filtered().length === 0 && catalog().length === 0) {
        <div class="kap-empty">
          <i class="pi pi-box kap-empty-icon"></i>
          <p>No kit items found.</p>
        </div>
      }

      @else if (filtered().length === 0 && filterText) {
        <div class="kap-empty">
          <i class="pi pi-filter kap-empty-icon"></i>
          <p>No items match "{{ filterText }}".</p>
          <button class="btn btn-secondary btn-sm" (click)="filterText = ''">Clear filter</button>
        </div>
      }

      <!-- ── Cards grid ──────────────────────────────────────────────────── -->
      @else {
        <div class="kap-grid">
          @for (item of filtered(); track item.kitItemId) {
            <div class="kap-card"
                 [class.kap-card--available]="item.availableQuantity > 0 && availPct(item) > 20"
                 [class.kap-card--low]="item.availableQuantity > 0 && availPct(item) <= 20"
                 [class.kap-card--out]="item.availableQuantity === 0">
              <!-- status stripe top-left -->
              <div class="kap-card-stripe"></div>

              <div class="kap-card-top">
                <span class="kap-category">{{ item.category }}</span>
                @if (item.overdueCheckouts > 0) {
                  <span class="kap-overdue-tag">
                    <i class="pi pi-exclamation-triangle"></i> {{ item.overdueCheckouts }} overdue
                  </span>
                }
              </div>

              <h3 class="kap-card-name">{{ item.name }}</h3>

              <!-- Availability bar -->
              <div class="kap-avail-bar-wrap">
                <div class="kap-avail-bar">
                  <div class="kap-avail-fill"
                       [style.width.%]="availPct(item)"
                       [class.kap-fill--available]="item.availableQuantity > 0 && availPct(item) > 20"
                       [class.kap-fill--low]="item.availableQuantity > 0 && availPct(item) <= 20"
                       [class.kap-fill--out]="item.availableQuantity === 0">
                  </div>
                </div>
                <span class="kap-avail-label"
                      [class.kap-avail--available]="item.availableQuantity > 0 && availPct(item) > 20"
                      [class.kap-avail--low]="item.availableQuantity > 0 && availPct(item) <= 20"
                      [class.kap-avail--out]="item.availableQuantity === 0">
                  {{ item.availableQuantity }} / {{ item.totalQuantity }}
                </span>
              </div>

              <div class="kap-card-footer">
                @if (item.availableQuantity === 0) {
                  <span class="kap-status-chip kap-status-chip--out">All checked out</span>
                } @else if (availPct(item) <= 20) {
                  <span class="kap-status-chip kap-status-chip--low">Limited availability</span>
                } @else {
                  <span class="kap-status-chip kap-status-chip--available">Available</span>
                }
                @if (item.outstandingCheckouts > 0) {
                  <span class="kap-checked-out-note">{{ item.outstandingCheckouts }} out</span>
                }
              </div>
            </div>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .kap-page {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    /* ── Header ─── */
    .kap-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }
    .kap-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .kap-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }
    .kap-header-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-shrink: 0;
    }
    .kap-last-updated {
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      white-space: nowrap;
    }

    /* ── Error banner ─── */
    .kap-error-banner {
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

    /* ── KPI strip ─── */
    .kap-kpis {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
    }
    @media (max-width: 640px) {
      .kap-kpis { grid-template-columns: repeat(2, 1fr); }
    }
    .kap-kpi {
      background: var(--stride-surface-card, #fff);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg, 0.75rem);
      padding: 1.125rem 1.25rem;
      text-align: center;
    }
    .kap-kpi--good  { border-color: var(--stride-success-border, #bbf7d0);
                      background: var(--stride-success-surface, #f0fdf4); }
    .kap-kpi--warn  { border-color: var(--stride-warning-border, #fde68a);
                      background: var(--stride-warning-surface, #fffbeb); }
    .kap-kpi--danger { border-color: var(--stride-danger-border, #fecaca);
                       background: var(--stride-danger-surface, #fef2f2); }
    .kap-kpi-value {
      font-size: 2rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      line-height: 1;
    }
    .kap-kpi-label {
      font-size: 0.75rem;
      color: var(--stride-text-secondary);
      margin-top: 0.375rem;
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    /* ── Filter bar ─── */
    .kap-filter-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }
    .kap-search-wrap {
      position: relative;
      flex: 1;
      max-width: 22rem;
    }
    .kap-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-secondary);
      font-size: 0.875rem;
      pointer-events: none;
    }
    .kap-search {
      width: 100%;
      box-sizing: border-box;
      padding: 0.5rem 2.25rem 0.5rem 2.25rem;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-md);
      font-size: 0.875rem;
      color: var(--stride-text-primary);
      background: var(--stride-surface-input, #fff);
      transition: border-color 150ms;
    }
    .kap-search:focus { outline: none; border-color: var(--stride-primary); }
    .kap-search-clear {
      position: absolute;
      right: 0.5rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      cursor: pointer;
      color: var(--stride-text-secondary);
      padding: 0.25rem;
      display: flex;
    }
    .kap-legend {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      flex-shrink: 0;
    }
    .kap-dot {
      display: inline-block;
      width: 0.625rem;
      height: 0.625rem;
      border-radius: 50%;
      margin-right: 0.25rem;
    }
    .kap-dot--available { background: var(--stride-success, #16a34a); }
    .kap-dot--low       { background: var(--stride-warning, #d97706); }
    .kap-dot--out       { background: var(--stride-text-secondary); }

    /* ── Card grid ─── */
    .kap-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 1rem;
    }

    .kap-card {
      position: relative;
      background: var(--stride-surface-card, #fff);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg, 0.75rem);
      padding: 1rem 1.125rem 0.875rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      overflow: hidden;
      transition: box-shadow 150ms, border-color 150ms;
    }
    .kap-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,.08); }

    /* Left colour stripe */
    .kap-card-stripe {
      position: absolute;
      left: 0; top: 0; bottom: 0;
      width: 4px;
      border-radius: 0.75rem 0 0 0.75rem;
      background: var(--stride-border);
    }
    .kap-card--available .kap-card-stripe { background: var(--stride-success, #16a34a); }
    .kap-card--low       .kap-card-stripe { background: var(--stride-warning, #d97706); }
    .kap-card--out       .kap-card-stripe { background: var(--stride-text-secondary, #94a3b8); }

    .kap-card-top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
    }
    .kap-category {
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--stride-primary);
      background: var(--stride-primary-surface, #eff6ff);
      padding: 0.15rem 0.5rem;
      border-radius: 999px;
      flex-shrink: 0;
    }
    .kap-overdue-tag {
      font-size: 0.6875rem;
      font-weight: 700;
      color: var(--stride-danger, #dc2626);
      display: flex;
      align-items: center;
      gap: 0.2rem;
    }
    .kap-card-name {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      margin: 0;
      line-height: 1.3;
    }

    /* Availability bar */
    .kap-avail-bar-wrap {
      display: flex;
      align-items: center;
      gap: 0.625rem;
    }
    .kap-avail-bar {
      flex: 1;
      height: 6px;
      background: var(--stride-border);
      border-radius: 999px;
      overflow: hidden;
    }
    .kap-avail-fill {
      height: 100%;
      border-radius: 999px;
      transition: width 300ms ease;
    }
    .kap-fill--available { background: var(--stride-success, #16a34a); }
    .kap-fill--low       { background: var(--stride-warning, #d97706); }
    .kap-fill--out       { background: var(--stride-text-secondary, #94a3b8); width: 0 !important; }

    .kap-avail-label {
      font-size: 0.875rem;
      font-weight: 600;
      min-width: 3rem;
      text-align: right;
      flex-shrink: 0;
    }
    .kap-avail--available { color: var(--stride-success, #16a34a); }
    .kap-avail--low       { color: var(--stride-warning, #d97706); }
    .kap-avail--out       { color: var(--stride-text-secondary); }

    .kap-card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
      margin-top: 0.25rem;
    }
    .kap-status-chip {
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
    }
    .kap-status-chip--available {
      background: var(--stride-success-surface, #f0fdf4);
      color: var(--stride-success, #16a34a);
    }
    .kap-status-chip--low {
      background: var(--stride-warning-surface, #fffbeb);
      color: var(--stride-warning, #d97706);
    }
    .kap-status-chip--out {
      background: var(--stride-surface-muted, #f1f5f9);
      color: var(--stride-text-secondary);
    }
    .kap-checked-out-note {
      font-size: 0.75rem;
      color: var(--stride-text-secondary);
    }

    /* ── Empty / skeleton ─── */
    .kap-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 4rem 1rem;
      color: var(--stride-text-secondary);
      gap: 0.75rem;
    }
    .kap-empty-icon { font-size: 2.5rem; opacity: .4; }

    .kap-skeleton {
      background: var(--stride-skeleton-base, #f1f5f9);
      border-radius: var(--stride-radius-md);
      animation: kap-pulse 1.4s ease-in-out infinite;
      min-height: 9rem;
    }
    @keyframes kap-pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: .5; }
    }

    /* ── Buttons ─── */
    .btn {
      display: inline-flex; align-items: center; gap: 0.375rem;
      padding: 0.5rem 1rem; border-radius: var(--stride-radius-md);
      font-size: 0.875rem; font-weight: 500; cursor: pointer;
      border: 1px solid transparent; transition: background 150ms, opacity 150ms;
    }
    .btn:disabled { opacity: .55; cursor: not-allowed; }
    .btn-sm { padding: 0.35rem 0.75rem; font-size: 0.8125rem; }
    .btn-secondary {
      background: var(--stride-surface-card, #f8fafc);
      color: var(--stride-text-primary);
      border-color: var(--stride-border);
    }
    .btn-secondary:hover:not(:disabled) { background: var(--stride-surface-hover, #f1f5f9); }

    /* ── PWA / mobile ─── */
    @media (max-width: 480px) {
      .kap-page { padding: 1rem; gap: 1rem; }
      .kap-grid { grid-template-columns: 1fr 1fr; }
      .kap-legend { display: none; }
    }
  `],
})
export class KitAvailabilityPageComponent implements OnInit {
  private readonly svc        = inject(KitOpsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly catalog    = signal<KitCatalogItemDto[]>([]);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);
  readonly lastUpdated = signal<Date | null>(null);

  filterText = '';

  readonly filtered = computed(() => {
    const q = this.filterText.toLowerCase().trim();
    if (!q) return this.catalog();
    return this.catalog().filter(
      i => i.name.toLowerCase().includes(q) || i.category.toLowerCase().includes(q)
    );
  });

  // KPIs
  readonly kpiTotalItems     = computed(() => this.catalog().length);
  readonly kpiAvailableUnits = computed(() =>
    this.catalog().reduce((s, i) => s + i.availableQuantity, 0));
  readonly kpiOutUnits       = computed(() =>
    this.catalog().reduce((s, i) => s + i.outstandingCheckouts, 0));
  readonly kpiOverdue        = computed(() =>
    this.catalog().reduce((s, i) => s + i.overdueCheckouts, 0));

  ngOnInit(): void {
    this.load();
    interval(REFRESH_INTERVAL_MS)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => {
          this.isLoading.set(true);
          return this.svc.getCatalog();
        }),
      )
      .subscribe({
        next:  items => { this.catalog.set(items); this.lastUpdated.set(new Date()); this.isLoading.set(false); },
        error: ()    => { this.error.set('Auto-refresh failed.'); this.isLoading.set(false); },
      });
  }

  refresh(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.svc.getCatalog().subscribe({
      next:  items => { this.catalog.set(items); this.lastUpdated.set(new Date()); this.isLoading.set(false); },
      error: ()    => { this.error.set('Failed to load kit availability.'); this.isLoading.set(false); },
    });
  }

  availPct(item: KitCatalogItemDto): number {
    if (item.totalQuantity === 0) return 0;
    return Math.round((item.availableQuantity / item.totalQuantity) * 100);
  }
}
