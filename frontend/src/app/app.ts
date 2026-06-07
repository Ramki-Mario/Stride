import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class App implements OnInit {
  // Inject ThemeService here to ensure it initialises on app boot
  // (applies data-theme attribute and listens for OS-preference changes).
  private readonly themeService = inject(ThemeService);

  ngOnInit(): void {
    // ThemeService constructor effect handles the initial apply.
    // Listen for OS-level theme changes while the app is open.
    if (globalThis.window !== undefined) {
      globalThis
        .matchMedia('(prefers-color-scheme: dark)')
        .addEventListener('change', (e) => {
          // Only follow OS if the user has no explicit stored preference
          const stored = this._getStoredTheme();
          if (!stored) {
            this.themeService.setTheme(e.matches ? 'dark' : 'light');
          }
        });
    }
  }

  private _getStoredTheme(): string | null {
    try { return localStorage.getItem('stride-theme'); } catch { return null; }
  }
}
