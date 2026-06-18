import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import {
  PlatformAdminService,
  TenantDetail,
} from '../../services/platform-admin.service';

@Component({
  selector: 'app-tenant-detail-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pd-page">
      <nav class="pd-breadcrumb">
        <a routerLink="/platform-admin">Platform Admin</a>
        <span class="pd-sep">›</span>
        <span>{{ tenant()?.name ?? 'Loading…' }}</span>
      </nav>

      @if (loading()) {
        <div class="pd-loading">Loading tenant…</div>
      } @else if (error()) {
        <div class="pd-error">{{ error() }}</div>
      } @else if (tenant()) {
        <header class="pd-header">
          <div>
            <h1 class="pd-title">{{ tenant()!.name }}</h1>
            <p class="pd-subtitle">{{ tenant()!.slug }} · {{ tenant()!.plan }}</p>
          </div>
          <span class="pd-badge" [class.pd-badge--active]="tenant()!.isActive" [class.pd-badge--inactive]="!tenant()!.isActive">
            {{ tenant()!.isActive ? 'Active' : 'Inactive' }}
          </span>
        </header>

        <div class="pd-meta-grid">
          <div class="pd-meta-card">
            <span class="pd-meta-label">Created</span>
            <span class="pd-meta-value">{{ tenant()!.createdAt | date:'mediumDate' }}</span>
          </div>
          <div class="pd-meta-card">
            <span class="pd-meta-label">Last Activity</span>
            <span class="pd-meta-value">{{ tenant()!.lastActivityAt ? (tenant()!.lastActivityAt | date:'mediumDate') : '—' }}</span>
          </div>
          <div class="pd-meta-card">
            <span class="pd-meta-label">Seats</span>
            <span class="pd-meta-value">{{ tenant()!.seatCount }}</span>
          </div>
          <div class="pd-meta-card">
            <span class="pd-meta-label">Plan</span>
            <span class="pd-meta-value">{{ tenant()!.plan }}</span>
          </div>
        </div>

        <section class="pd-section">
          <h2 class="pd-section-title">Users ({{ tenant()!.users.length }})</h2>
          <div class="pd-table-wrap">
            <table class="pd-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (u of tenant()!.users; track u.id) {
                  <tr>
                    <td class="pd-name">{{ u.displayName }}</td>
                    <td class="pd-muted">{{ u.email }}</td>
                    <td>{{ u.roles.join(', ') || '—' }}</td>
                    <td>
                      <span class="pd-badge pd-badge--sm"
                            [class.pd-badge--active]="u.isActive"
                            [class.pd-badge--inactive]="!u.isActive">
                        {{ u.isActive ? 'Active' : 'Inactive' }}
                      </span>
                    </td>
                  </tr>
                } @empty {
                  <tr><td colspan="4" class="pd-empty-cell">No users yet.</td></tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      }
    </div>
  `,
  styles: [`
    .pd-page { padding: 2rem; max-width: 1100px; margin: 0 auto; }

    .pd-breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.8125rem;
      color: var(--stride-muted);
      margin-bottom: 1.5rem;
    }
    .pd-breadcrumb a { color: var(--stride-primary); text-decoration: none; }
    .pd-breadcrumb a:hover { text-decoration: underline; }
    .pd-sep { color: var(--stride-border); }

    .pd-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      margin-bottom: 1.5rem;
    }
    .pd-title  { font-size: 1.5rem; font-weight: 700; color: var(--stride-text); margin: 0 0 0.25rem; }
    .pd-subtitle { font-size: 0.875rem; color: var(--stride-muted); margin: 0; }

    .pd-meta-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .pd-meta-card {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .pd-meta-label { font-size: 0.6875rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: var(--stride-muted); }
    .pd-meta-value { font-size: 1.125rem; font-weight: 600; color: var(--stride-text); }

    .pd-section { margin-bottom: 2rem; }
    .pd-section-title { font-size: 1rem; font-weight: 600; color: var(--stride-text); margin: 0 0 0.75rem; }

    .pd-table-wrap {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      overflow: hidden;
    }
    .pd-table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    .pd-table th {
      text-align: left;
      padding: 0.75rem 1rem;
      border-bottom: 1px solid var(--stride-border);
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .pd-table td {
      padding: 0.875rem 1rem;
      border-bottom: 1px solid var(--stride-border);
      color: var(--stride-text);
    }
    .pd-table tr:last-child td { border-bottom: none; }
    .pd-name { font-weight: 550; }
    .pd-muted { color: var(--stride-muted); }
    .pd-empty-cell { text-align: center; color: var(--stride-muted); padding: 2rem; }

    .pd-badge {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.625rem;
      border-radius: var(--stride-radius-full);
      font-size: 0.6875rem;
      font-weight: 600;
      letter-spacing: 0.04em;
    }
    .pd-badge--sm { font-size: 0.625rem; padding: 0.2rem 0.5rem; }
    .pd-badge--active  { background: #dcfce7; color: #16a34a; }
    .pd-badge--inactive { background: #fee2e2; color: #dc2626; }

    .pd-loading, .pd-error { padding: 3rem; text-align: center; color: var(--stride-muted); }
    .pd-error { color: #dc2626; }

    @media (max-width: 768px) {
      .pd-meta-grid { grid-template-columns: repeat(2, 1fr); }
    }
  `],
})
export class TenantDetailPageComponent implements OnInit {
  private readonly svc   = inject(PlatformAdminService);
  private readonly route = inject(ActivatedRoute);

  readonly tenant  = signal<TenantDetail | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getTenant(id).subscribe({
      next:  (t) => { this.tenant.set(t); this.loading.set(false); },
      error: () => { this.error.set('Tenant not found.'); this.loading.set(false); },
    });
  }
}
