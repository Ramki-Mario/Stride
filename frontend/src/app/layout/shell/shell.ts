import { Component, ChangeDetectionStrategy } from '@angular/core';
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
      <!-- Sidebar: host element receives .stride-sidebar via host binding -->
      <app-sidebar />

      <!-- Main column: topbar + scrollable content -->
      <div class="stride-main">
        <!-- Topbar: host element receives .stride-topbar via host binding -->
        <app-topbar />

        <main class="stride-content stride-animate-in">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {}
