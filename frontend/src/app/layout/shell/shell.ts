import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar';
import { TopbarComponent } from '../topbar/topbar';

@Component({
  selector: 'app-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="stride-shell">
      <!-- Sidebar: overlay on mobile (mobileOpen), collapsible rail on desktop -->
      <app-sidebar
        [mobileOpen]="mobileNavOpen()"
        (navClose)="mobileNavOpen.set(false)" />

      <!-- Backdrop: closes mobile nav on tap-outside; hidden on desktop via CSS -->
      @if (mobileNavOpen()) {
        <div class="stride-nav-backdrop"
             (click)="mobileNavOpen.set(false)"
             aria-hidden="true"></div>
      }

      <!-- Main column: topbar + scrollable content -->
      <div class="stride-main">
        <app-topbar (hamburgerClick)="mobileNavOpen.set(true)" />

        <main class="stride-content stride-animate-in">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  protected readonly mobileNavOpen = signal(false);
}
