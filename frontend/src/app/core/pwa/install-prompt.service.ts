import { Injectable, signal } from '@angular/core';

/**
 * Captures the browser's `beforeinstallprompt` event and surfaces an
 * in-app "Add to Home Screen" prompt.  On iOS Safari (which does not fire
 * that event) we surface a manual instruction banner instead.
 */
@Injectable({ providedIn: 'root' })
export class InstallPromptService {
  /** True after the user has dismissed the banner once this session. */
  readonly dismissed = signal(false);

  /** Chrome/Edge/Android: native prompt deferred for later use. */
  private _deferredPrompt: BeforeInstallPromptEvent | null = null;

  /** Whether the OS-level install prompt is available. */
  readonly canPromptInstall = signal(false);

  /** True when running on iOS Safari (no beforeinstallprompt support). */
  readonly isIos = signal(false);

  /** True when the app is already running in standalone (installed) mode. */
  readonly isStandalone = signal(
    typeof window !== 'undefined' &&
    (window.matchMedia('(display-mode: standalone)').matches ||
     (navigator as Navigator & { standalone?: boolean }).standalone === true),
  );

  constructor() {
    if (typeof window === 'undefined') return;

    this.isIos.set(
      /iphone|ipad|ipod/.test(navigator.userAgent.toLowerCase()) &&
      !(window.navigator as Navigator & { standalone?: boolean }).standalone,
    );

    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      this._deferredPrompt = e as BeforeInstallPromptEvent;
      this.canPromptInstall.set(true);
    });

    window.addEventListener('appinstalled', () => {
      this._deferredPrompt = null;
      this.canPromptInstall.set(false);
      this.dismissed.set(true);
    });
  }

  /** Shows whether the install banner should be visible. */
  get shouldShowBanner(): boolean {
    if (this.isStandalone() || this.dismissed()) return false;
    return this.canPromptInstall() || this.isIos();
  }

  /** Triggers the native install prompt (Chromium). No-op on iOS. */
  async promptInstall(): Promise<void> {
    if (!this._deferredPrompt) return;
    await this._deferredPrompt.prompt();
    const outcome = await this._deferredPrompt.userChoice;
    if (outcome.outcome === 'accepted') this.dismissed.set(true);
    this._deferredPrompt = null;
    this.canPromptInstall.set(false);
  }

  dismiss(): void {
    this.dismissed.set(true);
  }
}

/** Extends the standard Event with the deferred install prompt API. */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}
