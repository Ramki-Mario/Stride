import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { AuthService } from '../../../../core/auth/auth.service';
import { ProfileService } from '../../services/profile.service';
import { RoleDetailDto } from '../../../administration/models/role.models';

const PERMISSION_LABELS: Record<string, string> = {
  'workflow.view':             'View Workflows',
  'workflow.create':           'Create & Edit Workflows',
  'workflow.activate':         'Activate / Archive Workflows',
  'workflow.run':              'Start Workflow Instances',
  'workflow.manage_instances': 'Manage All Instances',
  'user.invite':               'Invite Users',
  'user.manage':               'Manage Users & Roles',
  'role.view':                 'View Role Catalog',
  'role.manage':               'Create, Edit & Delete Roles',
  'tenant.settings':           'Edit Tenant Settings',
};

const DOMAIN_ORDER = ['workflow', 'user', 'role', 'tenant'];
const DOMAIN_LABELS: Record<string, string> = {
  workflow: 'Workflows',
  user:     'Users',
  role:     'Roles',
  tenant:   'Tenant',
};

interface PermissionGroup {
  domain: string;
  label:  string;
  keys:   string[];
}

@Component({
  selector: 'app-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pp-page">

      <!-- ── Header ──────────────────────────────────────────────────────── -->
      <div class="pp-header">
        <div class="pp-avatar">{{ initials() }}</div>
        <div class="pp-header-text">
          <h1 class="pp-name">{{ displayName() }}</h1>
          <p class="pp-email">{{ auth.user()?.email }}</p>
          @if (auth.user()?.tenantName) {
            <span class="pp-tenant">{{ auth.user()?.tenantName }}</span>
          }
        </div>
      </div>

      <!-- ── My Access ───────────────────────────────────────────────────── -->
      <section class="pp-section">
        <h2 class="pp-section-title">My Access</h2>
        <p class="pp-section-sub">Your assigned roles and the permissions each one grants.</p>

        @if (loading()) {
          <div class="pp-skeleton-list">
            @for (i of [1,2,3]; track i) {
              <div class="pp-sk-card">
                <div class="sk-line sk-line--title"></div>
                <div class="sk-line sk-line--desc"></div>
                <div class="sk-line sk-line--perms"></div>
              </div>
            }
          </div>

        } @else if (error()) {
          <div class="pp-empty">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
              <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <p>{{ error() }}</p>
            <button class="stride-btn stride-btn-secondary" (click)="loadRoles()">Retry</button>
          </div>

        } @else if (roles().length === 0) {
          <div class="pp-empty">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <rect x="3" y="11" width="18" height="11" rx="2" stroke="currentColor" stroke-width="1.5"/>
              <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            <p>No roles have been assigned to your account yet.</p>
          </div>

        } @else {
          <div class="pp-role-list">
            @for (role of roles(); track role.id) {
              <div class="pp-role-card">

                <!-- Role header -->
                <div class="pp-role-header" (click)="toggleRole(role.id)">
                  <div class="pp-role-meta">
                    <div class="pp-role-name-row">
                      <span class="pp-role-name">{{ role.name }}</span>
                      @if (role.isSystemRole) {
                        <span class="pp-badge pp-badge--system">System</span>
                      } @else {
                        <span class="pp-badge pp-badge--custom">Custom</span>
                      }
                    </div>
                    @if (role.description) {
                      <p class="pp-role-desc">{{ role.description }}</p>
                    }
                    <p class="pp-role-perm-count">
                      {{ role.permissions.length }}
                      permission{{ role.permissions.length === 1 ? '' : 's' }}
                    </p>
                  </div>
                  <svg class="pp-chevron"
                       [class.pp-chevron--open]="expandedIds().has(role.id)"
                       width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2"
                          stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>

                <!-- Expanded permission breakdown -->
                @if (expandedIds().has(role.id)) {
                  <div class="pp-perms">
                    @if (role.permissions.length === 0) {
                      <p class="pp-no-perms">This role has no permissions assigned.</p>
                    } @else {
                      @for (group of permGroupsFor(role); track group.domain) {
                        <div class="pp-perm-group">
                          <div class="pp-perm-group-label">{{ group.label }}</div>
                          <div class="pp-perm-chips">
                            @for (key of group.keys; track key) {
                              <span class="pp-perm-chip">{{ permLabel(key) }}</span>
                            }
                          </div>
                        </div>
                      }
                    }
                  </div>
                }

              </div>
            }
          </div>
        }
      </section>

    </div>
  `,
  styles: [`
    .pp-page {
      max-width: 52rem;
      margin: 0 auto;
      padding: 1.75rem 1rem 3rem;
    }

    /* ── Header ──────────────────────────────────────────────────────────── */
    .pp-header {
      display: flex;
      align-items: center;
      gap: 1.25rem;
      margin-bottom: 2rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--stride-border-soft);
    }

    .pp-avatar {
      width: 4rem;
      height: 4rem;
      border-radius: 50%;
      background: var(--stride-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-size: 1.25rem;
      font-weight: 700;
      letter-spacing: 0.02em;
      flex-shrink: 0;
    }

    .pp-header-text { display: flex; flex-direction: column; gap: 0.15rem; }

    .pp-name {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0;
    }

    .pp-email {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }

    .pp-tenant {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-primary);
      background: color-mix(in srgb, var(--stride-primary) 10%, transparent);
      padding: 0.125rem 0.5rem;
      border-radius: 99px;
      width: fit-content;
      margin-top: 0.25rem;
    }

    /* ── Section ─────────────────────────────────────────────────────────── */
    .pp-section { display: flex; flex-direction: column; gap: 1rem; }

    .pp-section-title {
      font-size: 1rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0;
    }

    .pp-section-sub {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: -0.5rem 0 0;
    }

    /* ── Role cards ──────────────────────────────────────────────────────── */
    .pp-role-list { display: flex; flex-direction: column; gap: 0.75rem; }

    .pp-role-card {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: hidden;
    }

    .pp-role-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1rem 1.25rem;
      cursor: pointer;
      user-select: none;
      transition: background 120ms;
    }

    .pp-role-header:hover { background: var(--stride-surface-hover); }

    .pp-role-meta { display: flex; flex-direction: column; gap: 0.25rem; flex: 1; min-width: 0; }

    .pp-role-name-row { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }

    .pp-role-name {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--stride-text-primary);
    }

    .pp-role-desc {
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }

    .pp-role-perm-count {
      font-size: 0.75rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    .pp-chevron {
      color: var(--stride-text-muted);
      flex-shrink: 0;
      transition: transform 200ms;
    }

    .pp-chevron--open { transform: rotate(180deg); }

    /* ── Badge ───────────────────────────────────────────────────────────── */
    .pp-badge {
      font-size: 0.625rem;
      font-weight: 700;
      padding: 0.125rem 0.45rem;
      border-radius: 99px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .pp-badge--system {
      background: color-mix(in srgb, #6B7280 12%, transparent);
      color: #6B7280;
    }

    .pp-badge--custom {
      background: color-mix(in srgb, #10B981 12%, transparent);
      color: #059669;
    }

    /* ── Expanded permissions ────────────────────────────────────────────── */
    .pp-perms {
      border-top: 1px solid var(--stride-border-soft);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
      background: var(--stride-surface-secondary);
    }

    .pp-no-perms {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    .pp-perm-group { display: flex; flex-direction: column; gap: 0.4rem; }

    .pp-perm-group-label {
      font-size: 0.6875rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--stride-text-muted);
    }

    .pp-perm-chips { display: flex; flex-wrap: wrap; gap: 0.35rem; }

    .pp-perm-chip {
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 500;
      padding: 0.2rem 0.6rem;
      border-radius: 99px;
      background: color-mix(in srgb, var(--stride-primary) 10%, transparent);
      color: var(--stride-primary);
      white-space: nowrap;
    }

    /* ── Empty / error ───────────────────────────────────────────────────── */
    .pp-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 3rem 1rem;
      color: var(--stride-text-muted);
      font-size: 0.9375rem;
      text-align: center;
    }

    /* ── Skeleton ─────────────────────────────────────────────────────────── */
    .pp-skeleton-list { display: flex; flex-direction: column; gap: 0.75rem; }

    .pp-sk-card {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      animation: pulse 1.5s ease-in-out infinite;
    }

    .sk-line {
      border-radius: 4px;
      background: var(--stride-border);
      height: 0.75rem;
    }

    .sk-line--title  { width: 10rem; }
    .sk-line--desc   { width: 18rem; }
    .sk-line--perms  { width: 14rem; }

    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* ── Mobile ───────────────────────────────────────────────────────────── */
    @media (max-width: 640px) {
      .pp-page { padding: 1rem 0.75rem 2rem; }
      .pp-header { gap: 0.875rem; }
      .pp-avatar { width: 3rem; height: 3rem; font-size: 1rem; }
    }
  `],
})
export class ProfilePageComponent implements OnInit {
  protected readonly auth    = inject(AuthService);
  private   readonly svc     = inject(ProfileService);

  protected readonly roles       = signal<RoleDetailDto[]>([]);
  protected readonly loading     = signal(true);
  protected readonly error       = signal<string | null>(null);
  protected readonly expandedIds = signal<Set<string>>(new Set());

  protected readonly initials = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local  = email.split('@')[0];
    const parts  = local.split(/[._-]/);
    if (parts.length >= 2 && parts[0] && parts[1])
      return (parts[0][0] + parts[1][0]).toUpperCase();
    return local.substring(0, 2).toUpperCase() || 'U';
  });

  protected readonly displayName = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local  = email.split('@')[0];
    return local
      .split(/[._-]/)
      .map(p => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ') || 'User';
  });

  ngOnInit(): void { this.loadRoles(); }

  protected loadRoles(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getMyRoles().subscribe({
      next:  roles => { this.roles.set(roles); this.loading.set(false); },
      error: ()    => { this.error.set('Failed to load your roles.'); this.loading.set(false); },
    });
  }

  protected toggleRole(id: string): void {
    const s = new Set(this.expandedIds());
    s.has(id) ? s.delete(id) : s.add(id);
    this.expandedIds.set(s);
  }

  protected permLabel(key: string): string {
    return PERMISSION_LABELS[key] ?? key;
  }

  protected permGroupsFor(role: RoleDetailDto): PermissionGroup[] {
    const byDomain = new Map<string, string[]>();
    for (const p of role.permissions) {
      const domain = p.key.split('.')[0];
      const list = byDomain.get(domain) ?? [];
      list.push(p.key);
      byDomain.set(domain, list);
    }

    const groups: PermissionGroup[] = [];
    for (const domain of DOMAIN_ORDER) {
      const keys = byDomain.get(domain);
      if (keys?.length) {
        groups.push({
          domain,
          label: DOMAIN_LABELS[domain] ?? domain,
          keys,
        });
        byDomain.delete(domain);
      }
    }
    // Append any unknown domains not in DOMAIN_ORDER
    for (const [domain, keys] of byDomain) {
      groups.push({ domain, label: domain, keys });
    }
    return groups;
  }
}
