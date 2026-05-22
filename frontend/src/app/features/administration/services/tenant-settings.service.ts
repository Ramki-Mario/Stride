import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TenantSettingsDto,
  UpdateTenantSettingsRequest,
  SanitisedCssResult,
} from '../models/tenant-settings.models';

@Injectable({ providedIn: 'root' })
export class TenantSettingsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/administration/settings';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly settings   = signal<TenantSettingsDto | null>(null);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);

  // ── Load ──────────────────────────────────────────────────────────────────

  loadSettings(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.http.get<TenantSettingsDto>(this.base).subscribe({
      next:  s  => { this.settings.set(s); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load settings.'); this.isLoading.set(false); },
    });
  }

  // ── Update ─────────────────────────────────────────────────────────────────

  /** Settings-only update (no CSS). Returns void Observable. */
  updateSettings(req: UpdateTenantSettingsRequest): Observable<void> {
    return this.http.put<void>(this.base, req);
  }

  /**
   * Update settings including raw CSS text.
   * Server sanitises and returns SanitisedCssResult with accepted + rejected entries.
   */
  updateSettingsWithCss(req: UpdateTenantSettingsRequest): Observable<SanitisedCssResult> {
    return this.http.put<SanitisedCssResult>(this.base, req);
  }

  // ── CSS template download ─────────────────────────────────────────────────

  downloadCssTemplate(): void {
    this.http
      .get(`${this.base}/css-template`, { responseType: 'blob' })
      .subscribe(blob => {
        const url  = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href     = url;
        link.download = 'stride-theme-template.css';
        link.click();
        URL.revokeObjectURL(url);
      });
  }
}
