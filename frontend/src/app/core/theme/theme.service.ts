import { Injectable, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type ThemeMode    = 'light' | 'dark';
export type ThemePalette = 'indigo' | 'purple';

const MODE_STORAGE_KEY    = 'stride-theme';
const PALETTE_STORAGE_KEY = 'stride-palette';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser  = isPlatformBrowser(this.platformId);

  /** Current theme mode — read from localStorage, falls back to system preference */
  readonly mode = signal<ThemeMode>(this._resolveInitialTheme());

  /** Current colour palette — read from localStorage, defaults to 'indigo' */
  readonly palette = signal<ThemePalette>(this._resolveInitialPalette());

  /** True when dark mode is active */
  readonly isDark = computed(() => this.mode() === 'dark');

  constructor() {
    // Apply theme mode to <html> whenever the signal changes
    effect(() => {
      this._applyMode(this.mode());
    });

    // Apply colour palette to <html> whenever the signal changes
    effect(() => {
      this._applyPalette(this.palette());
    });
  }

  /** Toggle between light and dark */
  toggle(): void {
    this.mode.update(m => m === 'light' ? 'dark' : 'light');
  }

  /** Explicitly set the theme mode */
  setTheme(mode: ThemeMode): void {
    this.mode.set(mode);
  }

  /** Switch to a different colour palette */
  setPalette(palette: ThemePalette): void {
    this.palette.set(palette);
  }

  /**
   * Applies the tenant's server-stored default palette from the AuthUser signal.
   * Called in APP_INITIALIZER after checkSession() resolves so that the server-side
   * preference overrides the local localStorage value on every login/session restore.
   *
   * Accepts any string from the API; silently ignores unknown values (falls back to 'purple').
   */
  applyPaletteFromSession(rawPalette: string | undefined | null): void {
    const palette: ThemePalette =
      rawPalette === 'indigo' || rawPalette === 'purple' ? rawPalette : 'purple';
    this.setPalette(palette);
  }

  // ── Private helpers ────────────────────────────────────────────────────

  private _applyMode(mode: ThemeMode): void {
    if (!this.isBrowser) return;
    document.documentElement.dataset['theme'] = mode;
    try { localStorage.setItem(MODE_STORAGE_KEY, mode); } catch { /* private mode */ }
  }

  private _applyPalette(palette: ThemePalette): void {
    if (!this.isBrowser) return;
    // Purple is the root default — no attribute needed.
    // Indigo is the alternative — requires data-palette="indigo".
    if (palette === 'purple') {
      delete document.documentElement.dataset['palette'];
    } else {
      document.documentElement.dataset['palette'] = palette;
    }
    try { localStorage.setItem(PALETTE_STORAGE_KEY, palette); } catch { /* private mode */ }
  }

  private _resolveInitialTheme(): ThemeMode {
    if (!this.isBrowser) return 'light';
    try {
      const stored = localStorage.getItem(MODE_STORAGE_KEY) as ThemeMode | null;
      if (stored === 'light' || stored === 'dark') return stored;
    } catch { /* ignore */ }
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private _resolveInitialPalette(): ThemePalette {
    if (!this.isBrowser) return 'purple';
    try {
      const stored = localStorage.getItem(PALETTE_STORAGE_KEY) as ThemePalette | null;
      if (stored === 'indigo' || stored === 'purple') return stored;
    } catch { /* ignore */ }
    return 'purple';   // Purple is the new official default
  }
}
