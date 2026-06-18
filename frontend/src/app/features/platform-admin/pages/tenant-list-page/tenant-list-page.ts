import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import {
  PlatformAdminService,
  PlatformStats,
  TenantSummary,
} from '../../services/platform-admin.service';

@Component({
  selector: 'app-tenant-list-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pa-page">
      <header class="pa-header">
        <div>
          <h1 class="pa-title">Platform Admin</h1>
          <p class="pa-subtitle">All tenants on StrydeSuite</p>
        </div>
      </header>

      @if (stats()) {
        <div class="pa-stats-grid">
          <div class="pa-stat-card">
            <span class="pa-stat-value">{{ stats()!.totalTenants }}</span>
            <span class="pa-stat-label">Total Tenants</span>
          </div>
          <div class="pa-stat-card">
            <span class="pa-stat-value">{{ stats()!.activeTenants }}</span>
            <span class="pa-stat-label">Active</span>
          </div>
          <div class="pa-stat-card">
            <span class="pa-stat-value">{{ stats()!.totalUsers }}</span>
            <span class="pa-stat-label">Total Users</span>
          </div>
          <div class="pa-stat-card">
            <span class="pa-stat-value">{{ stats()!.newTenantsThisWeek }}</span>
            <span class="pa-stat-label">New This Week</span>
          </div>
        </div>
      }

      @if (error()) {
        <div class="pa-error">{{ error() }}</div>
      }

      @if (loading()) {
        <div class="pa-loading">Loading tenants…</div>
      } @else if (tenants().length === 0 && !error()) {
        <div class="pa-empty">No tenants yet.</div>
      } @else {
        <div class="pa-table-wrap">
          <table class="pa-table">
            <thead>
              <tr>
                <th>Tenant</th>
                <th>Slug</th>
                <th>Plan</th>
                <th>Seats</th>
                <th>Status</th>
                <th>Created</th>
                <th>Last Active</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (t of tenants(); track t.id) {
                <tr>
                  <td class="pa-name">{{ t.name }}</td>
                  <td class="pa-muted">{{ t.slug }}</td>
                  <td>{{ t.plan }}</td>
                  <td>{{ t.seatCount }}</td>
                  <td>
                    <span class="pa-badge" [class.pa-badge--active]="t.isActive" [class.pa-badge--inactive]="!t.isActive">
                      {{ t.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="pa-muted">{{ t.createdAt | date:'mediumDate' }}</td>
                  <td class="pa-muted">{{ t.lastActivityAt ? (t.lastActivityAt | date:'mediumDate') : '—' }}</td>
                  <td>
                    <a class="pa-view-link" [routerLink]="['tenants', t.id]">View →</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .pa-page { padding: 2rem; max-width: 1200px; margin: 0 auto; }

    .pa-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      margin-bottom: 1.5rem;
    }
    .pa-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text);
      margin: 0 0 0.25rem;
    }
    .pa-subtitle { font-size: 0.875rem; color: var(--stride-muted); margin: 0; }

    .pa-stats-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      margin-bottom: 1.5rem;
    }
    .pa-stat-card {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      padding: 1.25rem 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .pa-stat-value {
      font-size: 2rem;
      font-weight: 700;
      color: var(--stride-text);
      line-height: 1;
    }
    .pa-stat-label { font-size: 0.75rem; color: var(--stride-muted); text-transform: uppercase; letter-spacing: 0.05em; }

    .pa-table-wrap {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      overflow: hidden;
    }
    .pa-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    .pa-table th {
      text-align: left;
      padding: 0.75rem 1rem;
      background: var(--stride-surface-alt, var(--stride-surface));
      border-bottom: 1px solid var(--stride-border);
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .pa-table td {
      padding: 0.875rem 1rem;
      border-bottom: 1px solid var(--stride-border);
      color: var(--stride-text);
      vertical-align: middle;
    }
    .pa-table tr:last-child td { border-bottom: none; }
    .pa-table tr:hover td { background: var(--stride-hover, rgba(0,0,0,.02)); }

    .pa-name { font-weight: 550; }
    .pa-muted { color: var(--stride-muted); }

    .pa-badge {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.625rem;
      border-radius: var(--stride-radius-full);
      font-size: 0.6875rem;
      font-weight: 600;
      letter-spacing: 0.04em;
    }
    .pa-badge--active  { background: #dcfce7; color: #16a34a; }
    .pa-badge--inactive { background: #fee2e2; color: #dc2626; }

    .pa-view-link {
      color: var(--stride-primary);
      text-decoration: none;
      font-size: 0.8125rem;
      font-weight: 500;
    }
    .pa-view-link:hover { text-decoration: underline; }

    .pa-loading, .pa-empty, .pa-error {
      padding: 3rem;
      text-align: center;
      color: var(--stride-muted);
      font-size: 0.9375rem;
    }
    .pa-error { color: #dc2626; }

    @media (max-width: 768px) {
      .pa-stats-grid { grid-template-columns: repeat(2, 1fr); }
    }
  `],
})
export class TenantListPageComponent implements OnInit {
  private readonly svc = inject(PlatformAdminService);

  readonly tenants = signal<TenantSummary[]>([]);
  readonly stats   = signal<PlatformStats | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  ngOnInit(): void {
    this.svc.getTenants().subscribe({
      next:  (t) => { this.tenants.set(t); this.loading.set(false); },
      error: () => { this.error.set('Failed to load tenants.'); this.loading.set(false); },
    });

    this.svc.getStats().subscribe({
      next:  (s) => this.stats.set(s),
      error: () => {},
    });
  }
}
