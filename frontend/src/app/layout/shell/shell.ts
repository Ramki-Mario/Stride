import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar';
import { TopbarComponent } from '../topbar/topbar';
import { ConnectivityService } from '../../core/pwa/connectivity.service';
import { InstallPromptService } from '../../core/pwa/install-prompt.service';
import { SyncService } from '../../core/pwa/sync.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="stride-shell">
      <!-- Sidebar -->
      <app-sidebar
        [mobileOpen]="mobileNavOpen()"
        (navClose)="mobileNavOpen.set(false)" />

      @if (mobileNavOpen()) {
        <div class="stride-nav-backdrop"
             (click)="mobileNavOpen.set(false)"
             aria-hidden="true"></div>
      }

      <div class="stride-main">
        <app-topbar (hamburgerClick)="mobileNavOpen.set(true)" />

        <!-- ── Offline banner ──────────────────────────────────────────────── -->
        @if (!connectivity.isOnline()) {
          <div class="stride-offline-banner" role="status" aria-live="polite">
            <span class="pi pi-wifi"></span>
            <span>
              You're offline. Step completions will be saved and synced when
              connectivity is restored.
            </span>
          </div>
        }

        <!-- ── Sync in-progress banner ────────────────────────────────────── -->
        @if (sync.isSyncing()) {
          <div class="stride-sync-banner" role="status" aria-live="polite">
            <span class="stride-sync-spinner" aria-hidden="true"></span>
            Syncing {{ sync.pendingCount() }} queued completion(s)…
          </div>
        }

        <!-- ── Sync error banner ──────────────────────────────────────────── -->
        @if (sync.lastError() && !sync.isSyncing()) {
          <div class="stride-sync-error-banner" role="alert">
            <span class="pi pi-exclamation-triangle"></span>
            {{ sync.lastError() }}
            <button class="stride-banner-dismiss"
                    (click)="sync.lastError.set(null)"
                    aria-label="Dismiss">×</button>
          </div>
        }

        <!-- ── Install banner — Android (native prompt) ───────────────────── -->
        @if (install.canPromptInstall() && !install.dismissed()) {
          <div class="stride-install-banner" role="complementary">
            <span class="pi pi-mobile"></span>
            <span>Install STRIDE on your home screen for the best experience.</span>
            <button class="stride-install-btn" (click)="install.promptInstall()">
              Install
            </button>
            <button class="stride-banner-dismiss"
                    (click)="install.dismiss()"
                    aria-label="Dismiss">×</button>
          </div>
        }

        <!-- ── Install banner — iOS (manual instruction) ─────────────────── -->
        @if (install.isIos() && !install.isStandalone() && !install.dismissed()) {
          <div class="stride-install-banner" role="complementary">
            <span class="pi pi-mobile"></span>
            <span>
              To install: tap <strong>Share</strong>
              <span class="pi pi-upload" aria-hidden="true"></span>
              then <strong>Add to Home Screen</strong>.
            </span>
            <button class="stride-banner-dismiss"
                    (click)="install.dismiss()"
                    aria-label="Dismiss">×</button>
          </div>
        }

        <main class="stride-content stride-animate-in">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent implements OnInit {
  protected readonly mobileNavOpen = signal(false);
  protected readonly connectivity  = inject(ConnectivityService);
  protected readonly install       = inject(InstallPromptService);
  protected readonly sync          = inject(SyncService);

  ngOnInit(): void {
    // Drain any queued completions if we're online at startup
    if (this.connectivity.isOnline()) {
      this.sync.drain();
    }
  }
}
