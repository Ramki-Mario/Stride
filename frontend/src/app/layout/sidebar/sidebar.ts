import {
  Component,
  ChangeDetectionStrategy,
  signal,
  inject,
  computed,
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;   // PrimeIcons class name (e.g. 'pi-objects-column')
  exact?: boolean;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  // Apply the structural class + collapsed modifier directly to the host element
  // so the global styles.scss .stride-sidebar rules take effect.
  host: {
    '[class.stride-sidebar]': 'true',
    '[class.collapsed]': 'collapsed()',
  },
  template: `
    <!-- ── Collapse toggle — absolutely positioned so it stays visible
         even when the sidebar is collapsed (overflow:hidden clips flex children
         that extend past 3.75rem, but position:absolute is clipped to the
         sidebar box which always fits a 1.625rem button at right:0.625rem) ── -->
    <button class="sb-collapse-btn"
            (click)="toggleCollapse()"
            [attr.aria-label]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'">
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none"
           [style.transform]="collapsed() ? 'rotate(180deg)' : 'rotate(0deg)'"
           style="transition: transform 200ms cubic-bezier(0.4,0,0.2,1)">
        <path d="M15 18l-6-6 6-6"
              stroke="currentColor" stroke-width="2.5"
              stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
    </button>

    <!-- ── Brand ──────────────────────────────────────────── -->
    <div class="sb-brand">
      <div class="sb-logo" aria-hidden="true">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
          <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"
                fill="white" stroke="white" stroke-width="1" stroke-linejoin="round"/>
        </svg>
      </div>

      <span class="sb-brand-name">STRIDE</span>
    </div>

    <!-- ── Tenant badge ──────────────────────────────────── -->
    <div class="sb-tenant">
      <span class="sb-tenant-dot" aria-hidden="true"></span>
      <span class="sb-tenant-name">STRIDE Demo</span>
    </div>

    <!-- ── Navigation ───────────────────────────────────── -->
    <nav class="sb-nav" aria-label="Main navigation">
      @for (group of navGroups; track group.label) {
        <div class="sb-group-label" aria-hidden="true">{{ group.label }}</div>

        @for (item of group.items; track item.route) {
          <a class="sb-nav-item"
             [routerLink]="item.route"
             routerLinkActive="active"
             [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
             [attr.aria-label]="item.label"
             [title]="collapsed() ? item.label : ''">
            <i class="pi {{ item.icon }} sb-nav-icon" aria-hidden="true"></i>
            <span class="sb-nav-label">{{ item.label }}</span>
          </a>
        }
      }
    </nav>

    <!-- ── User strip ─────────────────────────────────────── -->
    <div class="sb-user">
      <div class="sb-user-avatar" aria-hidden="true">{{ userInitials() }}</div>
      <div class="sb-user-info">
        <div class="sb-user-name" [title]="user()?.email ?? ''">
          {{ displayName() }}
        </div>
        <div class="sb-user-role">{{ userRole() }}</div>
      </div>
    </div>
  `,
  styles: [`
    /* ── Collapse button — absolutely positioned at brand-row height,
         right-aligned. Always within the sidebar box (even at 3.75rem)
         because right:0.5rem + button 1.625rem = 2.125rem < 3.75rem. ── */
    :host {
      position: relative;   /* anchor for the absolute collapse button */
      border-right: 1px solid var(--stride-nav-border);
    }

    .sb-collapse-btn {
      position: absolute;
      top: 1.0rem;          /* vertically centred in the 3.75rem brand area */
      right: 0.5rem;
      width: 1.625rem;
      height: 1.625rem;
      border-radius: var(--stride-radius-sm);
      border: none;
      background: transparent;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--stride-nav-muted);
      padding: 0;
      z-index: 1;
      transition: background 150ms, color 150ms;
    }
    .sb-collapse-btn:hover {
      background: var(--stride-nav-item-hover);
      color: var(--stride-nav-text);
    }

    /* ── Brand row ──────────────────────────────────────────── */
    .sb-brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      /* Right padding leaves room for the absolutely-positioned collapse btn */
      padding: 1.125rem 2.5rem 1.125rem 1rem;
      border-bottom: 1px solid var(--stride-nav-border);
      flex-shrink: 0;
      transition: border-color var(--stride-transition);
    }

    .sb-logo {
      width: 2rem;
      height: 2rem;
      background: var(--stride-primary);
      border-radius: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .sb-brand-name {
      flex: 1;
      font-size: 1.0625rem;
      font-weight: 700;
      color: var(--stride-nav-brand-text);
      letter-spacing: 0.03em;
      white-space: nowrap;
      overflow: hidden;
      transition: opacity 200ms cubic-bezier(0.4,0,0.2,1),
                  max-width 200ms cubic-bezier(0.4,0,0.2,1),
                  color var(--stride-transition);
      max-width: 8rem;
    }

    :host(.collapsed) .sb-brand-name {
      opacity: 0;
      max-width: 0;
    }

    /* ── Tenant badge ─────────────────────────────────────── */
    .sb-tenant {
      margin: 0.625rem 0.75rem;
      padding: 0.5rem 0.75rem;
      background: var(--stride-nav-item-active);
      border: 1px solid var(--stride-nav-border);
      border-radius: 0.625rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-shrink: 0;
      overflow: hidden;
    }

    .sb-tenant-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background: var(--stride-accent);
      flex-shrink: 0;
    }

    .sb-tenant-name {
      font-size: 0.75rem;
      font-weight: 500;
      color: var(--stride-nav-text);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      transition: opacity 200ms cubic-bezier(0.4,0,0.2,1);
    }

    :host(.collapsed) .sb-tenant-name {
      opacity: 0;
    }

    /* ── Nav ─────────────────────────────────────────────── */
    .sb-nav {
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 0.5rem 0;
      scrollbar-width: thin;
      scrollbar-color: var(--stride-nav-border) transparent;
    }

    .sb-group-label {
      font-size: 0.625rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: var(--stride-nav-muted);
      padding: 0.75rem 1rem 0.25rem;
      white-space: nowrap;
      overflow: hidden;
      transition: opacity 200ms cubic-bezier(0.4,0,0.2,1);
    }

    :host(.collapsed) .sb-group-label {
      opacity: 0;
    }

    .sb-nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5625rem 0.875rem;
      margin: 0.1rem 0.375rem;
      border-radius: var(--stride-radius-md);
      cursor: pointer;
      color: var(--stride-nav-text);
      font-size: 0.875rem;
      font-weight: 450;
      white-space: nowrap;
      position: relative;
      text-decoration: none;
      transition: background 150ms, color 150ms;
    }

    .sb-nav-item:hover {
      background: var(--stride-nav-item-hover);
      color: var(--stride-nav-active-text);
    }

    .sb-nav-item.active {
      background: var(--stride-nav-item-active);
      color: var(--stride-nav-active-text);
      font-weight: 550;
    }

    /* Active left-bar indicator */
    .sb-nav-item.active::before {
      content: '';
      position: absolute;
      left: -0.375rem;
      top: 50%;
      transform: translateY(-50%);
      width: 3px;
      height: 65%;
      background: var(--stride-nav-active-text);
      border-radius: 2px;
    }

    .sb-nav-icon {
      font-size: 0.9375rem;
      width: 1.25rem;
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--stride-nav-icon);
      transition: color 150ms;
    }

    .sb-nav-item:hover .sb-nav-icon,
    .sb-nav-item.active .sb-nav-icon {
      color: currentColor;
    }

    .sb-nav-label {
      flex: 1;
      overflow: hidden;
      transition: opacity 200ms cubic-bezier(0.4,0,0.2,1),
                  max-width 200ms cubic-bezier(0.4,0,0.2,1);
      max-width: 10rem;
    }

    :host(.collapsed) .sb-nav-label {
      opacity: 0;
      max-width: 0;
    }

    /* ── User strip ──────────────────────────────────────── */
    .sb-user {
      border-top: 1px solid var(--stride-nav-border);
      padding: 0.75rem;
      flex-shrink: 0;
      display: flex;
      align-items: center;
      gap: 0.625rem;
      overflow: hidden;
      transition: background 150ms;
      cursor: default;
    }

    .sb-user:hover {
      background: var(--stride-nav-item-hover);
    }

    .sb-user-avatar {
      width: 1.875rem;
      height: 1.875rem;
      border-radius: 50%;
      background: var(--stride-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-weight: 600;
      font-size: 0.6875rem;
      flex-shrink: 0;
      letter-spacing: 0.02em;
    }

    .sb-user-info {
      overflow: hidden;
      transition: opacity 200ms cubic-bezier(0.4,0,0.2,1),
                  max-width 200ms cubic-bezier(0.4,0,0.2,1);
      max-width: 8rem;
    }

    :host(.collapsed) .sb-user-info {
      opacity: 0;
      max-width: 0;
    }

    .sb-user-name {
      font-size: 0.8125rem;
      font-weight: 550;
      color: var(--stride-nav-text);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sb-user-role {
      font-size: 0.6875rem;
      color: var(--stride-nav-muted);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `],
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);

  /** Drives the collapsed CSS modifier on the host element. */
  protected readonly collapsed = signal(false);

  /** Authenticated user from the session signal. */
  protected readonly user = this.auth.user;

  /** Two-letter initials derived from the user's email. */
  protected readonly userInitials = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local = email.split('@')[0];
    const parts  = local.split(/[._-]/);
    if (parts.length >= 2 && parts[0] && parts[1]) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return local.substring(0, 2).toUpperCase() || 'U';
  });

  /** Friendly display name (local part of email, dots → space). */
  protected readonly displayName = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local = email.split('@')[0];
    return local
      .split(/[._-]/)
      .map(p => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ') || 'User';
  });

  /** First role in the roles array, formatted for display. */
  protected readonly userRole = computed(() => {
    const roles = this.auth.user()?.roles ?? [];
    if (!roles.length) return '';
    return roles[0].replace(/([A-Z])/g, ' $1').trim();
  });

  protected readonly navGroups: NavGroup[] = [
    {
      label: 'Main',
      items: [
        { label: 'Dashboard',     route: '/dashboard',     icon: 'pi-objects-column', exact: true },
        { label: 'Workflows',     route: '/workflows',     icon: 'pi-sitemap' },
        { label: 'Scheduling',    route: '/scheduling',    icon: 'pi-calendar' },
        { label: 'Notifications', route: '/notifications', icon: 'pi-bell' },
      ],
    },
    {
      label: 'Reporting',
      items: [
        { label: 'Reports',   route: '/reporting', icon: 'pi-chart-bar' },
      ],
    },
    {
      label: 'Invoicing',
      items: [
        { label: 'Invoices', route: '/invoicing', icon: 'pi-file-edit' },
      ],
    },
    {
      label: 'Administration',
      items: [
        { label: 'Users',            route: '/administration',          icon: 'pi-users',      exact: true },
        { label: 'Tenant Settings', route: '/administration/settings', icon: 'pi-palette' },
        { label: 'System Health',   route: '/administration/health',   icon: 'pi-heart-fill' },
      ],
    },
  ];

  protected toggleCollapse(): void {
    this.collapsed.update(c => !c);
  }
}
