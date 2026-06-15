import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { switchMap, tap } from 'rxjs/operators';

export interface AuthUser {
  userId: string;
  email: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
  defaultPalette: string;
  tenantName: string;
  onboardingCompleted: boolean;
  enabledModules: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _user = signal<AuthUser | null>(null);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  /** Set to true from the Settings page to re-show the wizard for a completed tenant. */
  readonly showWizard = signal(false);

  /** Updates the cached session to reflect onboarding completion without an extra HTTP call. */
  markOnboardingComplete(): void {
    const u = this._user();
    if (u) this._user.set({ ...u, onboardingCompleted: true });
    this.showWizard.set(false);
  }

  constructor(private readonly http: HttpClient, private readonly router: Router) {}

  /** Called on app init — checks session cookie validity via BFF. */
  checkSession() {
    return this.http.get<AuthUser>('/bff/auth/me').pipe(
      tap((user) => this._user.set(user))
    );
  }

  /**
   * Posts credentials to BFF, then pipes into checkSession() so the user
   * signal is populated before the component's `next` callback fires.
   * Returns Observable<AuthUser> (same shape as checkSession).
   */
  login(email: string, password: string) {
    return this.http
      .post<void>('/bff/auth/login', { email, password })
      .pipe(switchMap(() => this.checkSession()));
  }

  logout() {
    return this.http.post<void>('/bff/auth/logout', {}).pipe(
      tap(() => {
        this._user.set(null);
        this.router.navigate(['/login']);
      })
    );
  }

  validateInviteToken(token: string) {
    return this.http.get<{ email: string; displayName: string }>(
      `/bff/auth/accept-invite/validate?token=${encodeURIComponent(token)}`
    );
  }

  acceptInvite(token: string, password: string) {
    return this.http
      .post<void>('/bff/auth/accept-invite', { token, password })
      .pipe(switchMap(() => this.checkSession()));
  }
}
