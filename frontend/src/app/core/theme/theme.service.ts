import { Injectable, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'stride-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  /** Current theme mode — read from localStorage, falls back to system preference */
  readonly mode = signal<ThemeMode>(this._resolveInitialTheme());

  /** True when dark mode is active */
  readonly isDark = computed(() => this.mode() === 'dark');

  constructor() {
    // Apply theme to <html> whenever the signal changes
    effect(() => {
      this._applyTheme(this.mode());
    });
  }

  /** Toggle between light and dark */
  toggle(): void {
    this.mode.update(m => m === 'light' ? 'dark' : 'light');
  }

  /** Explicitly set the theme */
  setTheme(mode: ThemeMode): void {
    this.mode.set(mode);
  }

  private _applyTheme(mode: ThemeMode): void {
    if (!this.isBrowser) return;
    const html = document.documentElement;
    html.setAttribute('data-theme', mode);
    // Persist choice
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // localStorage may be unavailable (private mode, etc.)
    }
  }

  private _resolveInitialTheme(): ThemeMode {
    if (!this.isBrowser) return 'light';

    // 1. Check persisted preference
    try {
      const stored = localStorage.getItem(STORAGE_KEY) as ThemeMode | null;
      if (stored === 'light' || stored === 'dark') return stored;
    } catch {
      // ignore
    }

    // 2. Fall back to OS preference
    if (window.matchMedia?.('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }

    return 'light';
  }
}
