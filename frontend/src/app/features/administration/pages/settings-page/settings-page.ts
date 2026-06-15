import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TenantSettingsService } from '../../services/tenant-settings.service';
import { SanitisedCssResult } from '../../models/tenant-settings.models';
import { AuthService } from '../../../../core/auth/auth.service';

/** DOM id of the injected preview style element. */
const PREVIEW_STYLE_ID = 'byot-preview';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="settings-page">

      <!-- ── Header ─────────────────────────────────────────────────────── -->
      <div class="settings-header">
        <div>
          <h1 class="settings-title">Tenant Settings</h1>
          <p class="settings-subtitle">Configure branding and appearance for your organisation.</p>
        </div>
      </div>

      <!-- ── Load error ─────────────────────────────────────────────────── -->
      @if (svc.error()) {
        <div class="stride-banner stride-banner--error" style="margin-bottom:1.5rem">
          {{ svc.error() }}
        </div>
      }

      @if (svc.isLoading()) {
        <div class="settings-skeleton">
          @for (_ of [1,2,3]; track $index) { <div class="skeleton-row"></div> }
        </div>

      } @else if (svc.settings()) {

        <!-- ── Section 1: Branding ────────────────────────────────────── -->
        <div class="settings-card">
          <h2 class="settings-section-title">Branding</h2>

          <div class="settings-field">
            <label class="settings-label" for="displayName">Display Name</label>
            <input id="displayName" class="stride-input" type="text" maxlength="200"
              placeholder="Your organisation name" [(ngModel)]="displayName" />
            <p class="settings-hint">Shown in the app header and emails sent to your users.</p>
          </div>

          <div class="settings-field">
            <label class="settings-label" for="timezone">Timezone</label>
            <input id="timezone" class="stride-input" type="text" maxlength="100"
              placeholder="UTC" [(ngModel)]="timezone" />
            <p class="settings-hint">IANA identifier, e.g. <code>Europe/London</code>, <code>America/New_York</code>.</p>
          </div>

          <div class="settings-field">
            <label class="settings-label">Colour Palette</label>
            <div class="palette-options">
              <button type="button" class="palette-option"
                [class.palette-option--active]="defaultPalette === 'purple'"
                (click)="defaultPalette = 'purple'">
                <span class="palette-swatch palette-swatch--purple"></span>Purple
              </button>
              <button type="button" class="palette-option"
                [class.palette-option--active]="defaultPalette === 'indigo'"
                (click)="defaultPalette = 'indigo'">
                <span class="palette-swatch palette-swatch--indigo"></span>Indigo
              </button>
            </div>
            <p class="settings-hint">Applied automatically for all users in your organisation on login.</p>
          </div>

          <div class="settings-actions">
            <button class="stride-btn stride-btn-primary" [disabled]="saving()"
              (click)="saveSettings()">
              {{ saving() ? 'Saving…' : 'Save Settings' }}
            </button>
            @if (saveSuccess()) { <span class="settings-save-ok">✓ Saved</span> }
            @if (saveError())   { <span class="settings-save-err">{{ saveError() }}</span> }
          </div>
        </div>

        <!-- ── Section 2: Custom CSS Tokens ──────────────────────────── -->
        <div class="settings-card" style="margin-top:1.5rem">

          <!-- Preview mode active banner ──────────────────────────────── -->
          @if (isPreviewing()) {
            <div class="preview-banner">
              <div class="preview-banner__left">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                  <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                  <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                <strong>Preview mode active</strong> — tokens injected locally, not yet saved to your account.
              </div>
              <div class="preview-banner__actions">
                <button class="stride-btn stride-btn-primary btn-sm" [disabled]="saving()"
                  (click)="saveCss()">
                  {{ saving() ? 'Saving…' : 'Save' }}
                </button>
                <button class="stride-btn stride-btn-ghost btn-sm" (click)="resetPreview()">
                  Reset
                </button>
              </div>
            </div>
          }

          <div class="byot-header">
            <div>
              <h2 class="settings-section-title">Custom CSS Tokens</h2>
              <p class="settings-hint" style="margin-top:0.25rem">
                Override individual design tokens for your organisation.
                Only <code>--stride-*</code> properties inside <code>:root&nbsp;&#123;&#125;</code> are applied.
              </p>
            </div>
            <button type="button" class="stride-btn stride-btn-secondary" (click)="downloadTemplate()">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true" style="margin-right:6px">
                <path d="M12 3v13M6 11l6 6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M4 20h16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
              Download Template
            </button>
          </div>

          <!-- File upload + textarea ───────────────────────────────────── -->
          <div class="byot-upload-row">
            <label class="byot-file-label" for="cssFileInput">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="17 8 12 3 7 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <line x1="12" y1="3" x2="12" y2="15" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
              Upload .css file
            </label>
            <input id="cssFileInput" type="file" accept=".css,text/css"
              style="display:none" (change)="onFileSelected($event)" />
            <span class="byot-or">or paste below</span>
          </div>

          <textarea class="byot-textarea" rows="12"
            placeholder=":root &#123;&#10;  --stride-primary: #B97AF9;&#10;  --stride-bg: #F7F4FA;&#10;&#125;"
            [(ngModel)]="rawCss">
          </textarea>

          <!-- Action row: Preview | Save | Clear ──────────────────────── -->
          <div class="byot-submit-row">
            <button class="stride-btn stride-btn-secondary"
              [disabled]="!rawCss.trim()"
              (click)="previewCss()"
              title="Inject tokens into the page immediately without saving">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true" style="margin-right:5px">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
              </svg>
              Preview
            </button>
            <button class="stride-btn stride-btn-primary"
              [disabled]="saving() || !rawCss.trim()"
              (click)="saveCss()">
              {{ saving() ? 'Saving…' : 'Save CSS' }}
            </button>
            <button class="stride-btn stride-btn-ghost" [disabled]="saving()"
              (click)="clearCss()">
              Clear
            </button>
          </div>

          <!-- Save feedback ────────────────────────────────────────────── -->
          @if (sanitiseResult()) {
            <div class="byot-feedback">

              @if (objectKeys(sanitiseResult()!.acceptedTokens).length > 0) {
                <div class="byot-feedback-section">
                  <p class="byot-feedback-title byot-feedback-title--success">
                    ✓ {{ objectKeys(sanitiseResult()!.acceptedTokens).length }} token(s) accepted and saved
                  </p>
                  <div class="byot-chips">
                    @for (key of objectKeys(sanitiseResult()!.acceptedTokens); track key) {
                      <span class="byot-chip byot-chip--accepted">
                        <code>{{ key }}</code>: {{ sanitiseResult()!.acceptedTokens[key] }}
                      </span>
                    }
                  </div>
                </div>
              }

              @if (sanitiseResult()!.rejectedEntries.length > 0) {
                <div class="byot-feedback-section" style="margin-top:0.75rem">
                  <p class="byot-feedback-title byot-feedback-title--warn">
                    ⚠ {{ sanitiseResult()!.rejectedEntries.length }} entry(s) rejected
                  </p>
                  <div class="byot-chips">
                    @for (entry of sanitiseResult()!.rejectedEntries; track entry) {
                      <span class="byot-chip byot-chip--rejected"><code>{{ entry }}</code></span>
                    }
                  </div>
                </div>
              }

            </div>
          }

          @if (uploadError()) {
            <div class="stride-banner stride-banner--error" style="margin-top:1rem">
              {{ uploadError() }}
            </div>
          }

        </div>

        <!-- ── Section 3: Setup Wizard ───────────────────────────────────── -->
        <div class="settings-card" style="margin-top:1.5rem">
          <h2 class="settings-section-title">Setup Wizard</h2>
          <p class="settings-hint" style="margin-bottom:1rem">
            Re-run the guided setup wizard to create roles and invite team members.
          </p>
          <button class="stride-btn stride-btn-secondary" (click)="launchWizard()">
            <span class="pi pi-play-circle" style="margin-right:.4rem"></span>
            Launch Setup Wizard
          </button>
        </div>

      }
    </div>
  `,
  styles: [`
    .settings-page     { max-width: 800px; margin: 0 auto; padding: 1.5rem; }
    .settings-header   { margin-bottom: 1.5rem; }
    .settings-title    { font-size: 1.5rem; font-weight: 700; color: var(--stride-text-primary); margin: 0 0 0.25rem; }
    .settings-subtitle { font-size: 0.875rem; color: var(--stride-text-secondary); margin: 0; }

    .settings-card {
      background: var(--stride-surface); border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-2xl); padding: 1.5rem;
      box-shadow: var(--stride-shadow-xs);
    }
    .settings-section-title { font-size: 1rem; font-weight: 600; color: var(--stride-text-primary); margin: 0 0 1.25rem; }
    .settings-field    { margin-bottom: 1.25rem; }
    .settings-label    { display: block; font-size: 0.875rem; font-weight: 500; color: var(--stride-text-primary); margin-bottom: 0.375rem; }
    .settings-hint     { font-size: 0.75rem; color: var(--stride-text-muted); margin: 0.375rem 0 0; }
    .settings-actions  { display: flex; align-items: center; gap: 0.75rem; padding-top: 0.5rem; }
    .settings-save-ok  { font-size: 0.875rem; color: var(--stride-success); }
    .settings-save-err { font-size: 0.875rem; color: var(--stride-error); }

    /* Skeleton */
    .settings-skeleton { display: flex; flex-direction: column; gap: 1rem; }
    .skeleton-row      { height: 3rem; background: var(--stride-surface-secondary); border-radius: var(--stride-radius-md); animation: pulse 1.5s ease-in-out infinite; }
    @keyframes pulse   { 0%,100%{opacity:1} 50%{opacity:.5} }

    /* Palette picker */
    .palette-options       { display: flex; gap: 0.75rem; flex-wrap: wrap; }
    .palette-option        { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; border: 1.5px solid var(--stride-border); border-radius: var(--stride-radius-xl); background: var(--stride-surface); font-size: 0.875rem; cursor: pointer; transition: border-color 120ms, background 120ms; }
    .palette-option:hover  { border-color: var(--stride-primary); background: var(--stride-primary-subtle); }
    .palette-option--active{ border-color: var(--stride-primary); background: var(--stride-primary-subtle); font-weight: 600; }
    .palette-swatch        { display: inline-block; width: 14px; height: 14px; border-radius: 50%; }
    .palette-swatch--purple{ background: #B97AF9; }
    .palette-swatch--indigo{ background: #6366f1; }

    /* Preview mode banner */
    .preview-banner {
      display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 0.75rem;
      background: var(--stride-warning-bg); border: 1px solid var(--stride-warning-border);
      border-radius: var(--stride-radius-lg); padding: 0.625rem 1rem;
      font-size: 0.8125rem; color: var(--stride-warning);
      margin-bottom: 1.25rem;
    }
    .preview-banner__left  { display: flex; align-items: center; gap: 0.5rem; }
    .preview-banner__actions{ display: flex; gap: 0.5rem; }
    .btn-sm { padding: 0.3rem 0.75rem !important; font-size: 0.8125rem !important; }

    /* BYOT section */
    .byot-header      { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1.25rem; }
    .byot-upload-row  { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.75rem; }
    .byot-file-label  { display: inline-flex; align-items: center; gap: 0.375rem; padding: 0.4rem 0.875rem; border: 1.5px solid var(--stride-border); border-radius: var(--stride-radius-xl); font-size: 0.8125rem; font-weight: 500; cursor: pointer; background: var(--stride-surface); color: var(--stride-text-primary); transition: border-color 120ms, background 120ms; }
    .byot-file-label:hover { border-color: var(--stride-primary); background: var(--stride-primary-subtle); }
    .byot-or          { font-size: 0.8125rem; color: var(--stride-text-muted); }
    .byot-textarea    { width: 100%; font-family: 'Fira Code', 'Cascadia Code', monospace; font-size: 0.8125rem; background: var(--stride-surface-secondary); border: 1.5px solid var(--stride-border); border-radius: var(--stride-radius-md); padding: 0.75rem; color: var(--stride-text-primary); resize: vertical; box-sizing: border-box; line-height: 1.6; transition: border-color 120ms; }
    .byot-textarea:focus { outline: none; border-color: var(--stride-border-focus); box-shadow: 0 0 0 3px var(--stride-primary-ring); }
    .byot-submit-row  { display: flex; gap: 0.625rem; margin-top: 0.75rem; flex-wrap: wrap; }

    /* Save feedback */
    .byot-feedback               { margin-top: 1.25rem; border-top: 1px solid var(--stride-border-soft); padding-top: 1rem; }
    .byot-feedback-title         { font-size: 0.8125rem; font-weight: 600; margin: 0 0 0.5rem; }
    .byot-feedback-title--success{ color: var(--stride-success); }
    .byot-feedback-title--warn   { color: var(--stride-warning); }
    .byot-chips                  { display: flex; flex-wrap: wrap; gap: 0.375rem; }
    .byot-chip                   { display: inline-flex; padding: 0.2rem 0.6rem; border-radius: var(--stride-radius-xl); font-size: 0.75rem; max-width: 100%; overflow-wrap: break-word; word-break: break-all; }
    .byot-chip code              { font-family: inherit; }
    .byot-chip--accepted         { background: var(--stride-success-bg); border: 1px solid var(--stride-success-border); color: var(--stride-success); }
    .byot-chip--rejected         { background: var(--stride-warning-bg); border: 1px solid var(--stride-warning-border); color: var(--stride-warning); }
  `],
})
export class SettingsPageComponent implements OnInit, OnDestroy {
  protected readonly svc  = inject(TenantSettingsService);
  private   readonly auth = inject(AuthService);

  // ── Branding form ──────────────────────────────────────────────────────────
  protected displayName    = '';
  protected defaultPalette = 'purple';
  protected timezone       = 'UTC';
  protected saving         = signal(false);
  protected saveSuccess    = signal(false);
  protected saveError      = signal<string | null>(null);

  // ── BYOT CSS ───────────────────────────────────────────────────────────────
  protected rawCss         = '';
  protected uploadError    = signal<string | null>(null);
  protected sanitiseResult = signal<SanitisedCssResult | null>(null);
  protected isPreviewing   = signal(false);

  protected readonly objectKeys = Object.keys;

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.svc.loadSettings();
    const poll = setInterval(() => {
      const s = this.svc.settings();
      if (s) {
        this.displayName    = s.displayName    ?? '';
        this.defaultPalette = s.defaultPalette ?? 'purple';
        this.timezone       = s.timezone       ?? 'UTC';
        clearInterval(poll);
      }
    }, 50);
  }

  ngOnDestroy(): void {
    // Always clear the preview style tag on navigation away.
    this.removePreviewStyle();
  }

  // ── Branding ───────────────────────────────────────────────────────────────

  protected saveSettings(): void {
    this.saving.set(true);
    this.saveSuccess.set(false);
    this.saveError.set(null);

    this.svc.updateSettings({
      displayName:    this.displayName,
      defaultPalette: this.defaultPalette,
      timezone:       this.timezone,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveSuccess.set(true);
        setTimeout(() => this.saveSuccess.set(false), 3000);
        this.svc.loadSettings();
      },
      error: () => {
        this.saving.set(false);
        this.saveError.set('Failed to save settings. Please try again.');
      },
    });
  }

  // ── BYOT: Preview (client-side, no API call) ───────────────────────────────

  protected previewCss(): void {
    if (!this.rawCss.trim()) return;

    const tokens = this.extractTokensClientSide(this.rawCss);
    if (Object.keys(tokens).length === 0) return;

    const cssBody = Object.entries(tokens)
      .map(([k, v]) => `  ${k}: ${v};`)
      .join('\n');

    this.injectPreviewStyle(`:root {\n${cssBody}\n}`);
    this.isPreviewing.set(true);
  }

  protected resetPreview(): void {
    this.removePreviewStyle();
    this.isPreviewing.set(false);
  }

  // ── BYOT: Save (calls API, sanitises server-side, persists) ───────────────

  protected saveCss(): void {
    if (!this.rawCss.trim()) return;

    this.saving.set(true);
    this.uploadError.set(null);
    this.sanitiseResult.set(null);

    this.svc.updateSettingsWithCss({
      displayName:    this.displayName,
      defaultPalette: this.defaultPalette,
      timezone:       this.timezone,
      customCss:      this.rawCss,
    }).subscribe({
      next: result => {
        this.saving.set(false);
        this.sanitiseResult.set(result);
        // Preview is now superseded by the persisted tokens — clear it.
        this.removePreviewStyle();
        this.isPreviewing.set(false);
        this.svc.loadSettings();
      },
      error: () => {
        this.saving.set(false);
        this.uploadError.set('Failed to save CSS. Please try again.');
      },
    });
  }

  // ── BYOT: helpers ──────────────────────────────────────────────────────────

  protected downloadTemplate(): void { this.svc.downloadCssTemplate(); }

  protected async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    this.rawCss = await file.text();
    // Auto-preview on file select for instant feedback.
    this.previewCss();
    input.value = '';
  }

  protected clearCss(): void {
    this.rawCss = '';
    this.sanitiseResult.set(null);
    this.uploadError.set(null);
    this.resetPreview();
  }

  // ── Private preview helpers ────────────────────────────────────────────────

  /**
   * Lightweight client-side extractor — mirrors the server's deny-by-default
   * rules just enough to give a clean live preview. Does NOT replace server
   * sanitisation: the Save action always re-validates server-side.
   *
   * Accepts: --stride-* inside :root {}
   * Rejects: url(), expression(), @import, <script>, non-:root selectors
   */
  private extractTokensClientSide(css: string): Record<string, string> {
    const tokens: Record<string, string> = {};

    // Strip comments.
    const cleaned = css.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\/\/[^\r\n]*/g, '');

    // Extract :root { ... } blocks.
    const rootBlocks = [...cleaned.matchAll(/:root\s*\{([^}]*)\}/gi)];
    if (rootBlocks.length === 0) return tokens;

    const dangerousPattern = /url\s*\(|expression\s*\(|@import|<\s*script/i;

    for (const block of rootBlocks) {
      const body = block[1];
      for (const m of body.matchAll(/(--[a-zA-Z0-9-]+)\s*:\s*([^;]+);/gs)) {
        const name  = m[1].trim();
        const value = m[2].trim();
        if (!name.startsWith('--stride-'))       continue;
        if (dangerousPattern.test(value))         continue;
        tokens[name] = value;
      }
    }

    return tokens;
  }

  private injectPreviewStyle(cssText: string): void {
    let el = document.getElementById(PREVIEW_STYLE_ID) as HTMLStyleElement | null;
    if (!el) {
      el = document.createElement('style');
      el.id = PREVIEW_STYLE_ID;
      document.head.appendChild(el);
    }
    el.textContent = cssText;
  }

  private removePreviewStyle(): void {
    document.getElementById(PREVIEW_STYLE_ID)?.remove();
  }

  protected launchWizard(): void {
    this.auth.showWizard.set(true);
  }
}
