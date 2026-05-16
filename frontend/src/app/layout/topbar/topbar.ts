import {
  Component,
  ChangeDetectionStrategy,
  signal,
  inject,
  computed,
  ElementRef,
  HostListener,
} from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';

/** Map URL first-segment → human-readable section label. */
const ROUTE_LABELS: Record<string, string> = {
  dashboard:      'Dashboard',
  workflows:      'Workflows',
  scheduling:     'Scheduling',
  notifications:  'Notifications',
  reporting:      'Reports',
  administration: 'Administration',
};

@Component({
  selector: 'app-topbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  // Apply the structural class to the host element so styles.scss rules apply.
  host: { class: 'stride-topbar' },
  template: `
    <!-- ── Breadcrumb ───────────────────────────────────── -->
    <nav class="tb-breadcrumb" aria-label="Breadcrumb">
      <span class="tb-bc-root">STRIDE</span>
      <span class="tb-bc-sep" aria-hidden="true">/</span>
      <span class="tb-bc-current">{{ currentSection() }}</span>
    </nav>

    <!-- Spacer -->
    <div style="flex: 1"></div>

    <!-- ── Notification bell ────────────────────────────── -->
    <button class="tb-action" title="Notifications" aria-label="Notifications">
      <svg width="17" height="17" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"
              stroke="currentColor" stroke-width="1.8"
              stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
    </button>

    <!-- ── User menu ─────────────────────────────────────── -->
    <div class="tb-user-wrap">
      <button class="tb-user-trigger"
              (click)="toggleDropdown()"
              aria-haspopup="menu"
              [attr.aria-expanded]="dropdownOpen()">
        <div class="tb-avatar" aria-hidden="true">{{ userInitials() }}</div>
        <span class="tb-user-name">{{ displayName() }}</span>
        <svg class="tb-chevron" width="12" height="12" viewBox="0 0 24 24" fill="none"
             [style.transform]="dropdownOpen() ? 'rotate(180deg)' : 'rotate(0)'"
             style="transition: transform 150ms" aria-hidden="true">
          <path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2"
                stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>

      @if (dropdownOpen()) {
        <div class="tb-dropdown" role="menu" aria-label="User menu">
          <!-- Header -->
          <div class="tb-dd-header">
            <div class="tb-dd-name">{{ displayName() }}</div>
            <div class="tb-dd-email">{{ user()?.email }}</div>
          </div>

          <!-- Items -->
          <button class="tb-dd-item" role="menuitem">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M12 11a4 4 0 100-8 4 4 0 000 8z"
                    stroke="currentColor" stroke-width="1.8"
                    stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Profile
          </button>

          <button class="tb-dd-item" role="menuitem">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.8"/>
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"
                    stroke="currentColor" stroke-width="1.8"/>
            </svg>
            Settings
          </button>

          <hr class="tb-dd-divider" aria-hidden="true"/>

          <button class="tb-dd-item tb-dd-item--danger"
                  role="menuitem"
                  (click)="signOut()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9"
                    stroke="currentColor" stroke-width="1.8"
                    stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Sign out
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    /* ── Breadcrumb ─────────────────────────────────────── */
    .tb-breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.875rem;
    }

    .tb-bc-root {
      color: var(--stride-text-muted);
      font-weight: 500;
    }

    .tb-bc-sep {
      color: var(--stride-border);
    }

    .tb-bc-current {
      color: var(--stride-text-primary);
      font-weight: 600;
    }

    /* ── Action button (bell, etc.) ─────────────────────── */
    .tb-action {
      width: 2.25rem;
      height: 2.25rem;
      border-radius: var(--stride-radius-md);
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--stride-text-secondary);
      background: transparent;
      border: none;
      position: relative;
      transition: background 150ms, color 150ms;
    }

    .tb-action:hover {
      background: var(--stride-surface-hover);
      color: var(--stride-text-primary);
    }

    /* ── User menu wrapper ──────────────────────────────── */
    .tb-user-wrap {
      position: relative;
    }

    .tb-user-trigger {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0.625rem 0.25rem 0.25rem;
      border-radius: var(--stride-radius-lg);
      cursor: pointer;
      border: none;
      background: transparent;
      transition: background 150ms;
    }

    .tb-user-trigger:hover {
      background: var(--stride-surface-hover);
    }

    .tb-avatar {
      width: 2rem;
      height: 2rem;
      border-radius: 50%;
      background: var(--stride-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-weight: 600;
      font-size: 0.6875rem;
      letter-spacing: 0.02em;
      flex-shrink: 0;
    }

    .tb-user-name {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--stride-text-primary);
      white-space: nowrap;
    }

    .tb-chevron {
      color: var(--stride-text-muted);
    }

    /* ── Dropdown ────────────────────────────────────────── */
    .tb-dropdown {
      position: absolute;
      top: calc(100% + 0.5rem);
      right: 0;
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-lg);
      min-width: 13rem;
      padding: 0.375rem;
      z-index: 200;

      /* Entry animation */
      animation: dd-in 120ms cubic-bezier(0.4, 0, 0.2, 1) forwards;
    }

    @keyframes dd-in {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .tb-dd-header {
      padding: 0.625rem 0.75rem 0.5rem;
      border-bottom: 1px solid var(--stride-border-soft);
      margin-bottom: 0.25rem;
    }

    .tb-dd-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--stride-text-primary);
    }

    .tb-dd-email {
      font-size: 0.75rem;
      color: var(--stride-text-muted);
      margin-top: 0.1rem;
    }

    .tb-dd-item {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.5rem 0.75rem;
      border-radius: var(--stride-radius-md);
      font-size: 0.875rem;
      color: var(--stride-text-primary);
      cursor: pointer;
      transition: background 150ms;
      width: 100%;
      border: none;
      background: transparent;
      text-align: left;
      font-family: inherit;
    }

    .tb-dd-item:hover {
      background: var(--stride-surface-hover);
    }

    .tb-dd-item--danger {
      color: var(--stride-error);
    }

    .tb-dd-item--danger:hover {
      background: var(--stride-error-bg);
    }

    .tb-dd-divider {
      border: none;
      border-top: 1px solid var(--stride-border-soft);
      margin: 0.25rem 0;
    }
  `],
})
export class TopbarComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly el     = inject(ElementRef);

  protected readonly dropdownOpen = signal(false);
  protected readonly user         = this.auth.user;

  /** Reactive router URL — updates on every NavigationEnd. */
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map((e) => (e as NavigationEnd).urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  /** Human-readable section label derived from the first URL segment. */
  protected readonly currentSection = computed(() => {
    const url = this.currentUrl();
    const seg  = url.split('/').filter(Boolean)[0] ?? 'dashboard';
    return ROUTE_LABELS[seg] ?? (seg.charAt(0).toUpperCase() + seg.slice(1));
  });

  protected readonly userInitials = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local = email.split('@')[0];
    const parts  = local.split(/[._-]/);
    if (parts.length >= 2 && parts[0] && parts[1]) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return local.substring(0, 2).toUpperCase() || 'U';
  });

  protected readonly displayName = computed(() => {
    const email = this.auth.user()?.email ?? '';
    const local = email.split('@')[0];
    return local
      .split(/[._-]/)
      .map(p => p.charAt(0).toUpperCase() + p.slice(1))
      .join(' ') || 'User';
  });

  protected toggleDropdown(): void {
    this.dropdownOpen.update(o => !o);
  }

  protected signOut(): void {
    this.dropdownOpen.set(false);
    this.auth.logout().subscribe();
  }

  /** Close the user dropdown when clicking anywhere outside this component. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.dropdownOpen.set(false);
    }
  }
}
