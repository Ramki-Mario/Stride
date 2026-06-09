import {
  APP_INITIALIZER,
  ApplicationConfig,
  inject,
  isDevMode,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideServiceWorker } from '@angular/service-worker';
import { providePrimeNG } from 'primeng/config';
import { firstValueFrom, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { appRoutes } from './app.routes';
import { correlationInterceptor } from './core/http/correlation.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/theme/theme.service';

/**
 * Checks the BFF session cookie on application boot and applies the tenant's
 * server-stored default palette immediately after the session is confirmed.
 *
 * Angular waits for all APP_INITIALIZER factories to resolve before rendering
 * any routes, so authGuard's isAuthenticated() short-circuit will fire on the
 * very first navigation — no flicker or redirect to /login for logged-in users.
 *
 * catchError(() => of(null)) swallows ALL errors silently — both 401
 * (unauthenticated, normal) and network errors (BFF not running, offline).
 * The AuthService._user signal stays null; authGuard redirects to /login.
 */
function initSession() {
  const auth  = inject(AuthService);
  const theme = inject(ThemeService);
  return () => firstValueFrom(
    auth.checkSession().pipe(
      tap(user => {
        if (user) theme.applyPaletteFromSession(user.defaultPalette);
      }),
      catchError(() => of(null)),
    ),
    { defaultValue: null },
  );
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimationsAsync(),
    provideRouter(appRoutes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([correlationInterceptor, errorInterceptor])
    ),

    // PrimeNG — unstyled: false so component CSS is injected.
    // Actual colour tokens are set via CSS variables in styles.scss.
    providePrimeNG({
      ripple: true,
      inputVariant: 'outlined',
    }),

    // Session bootstrap — must resolve before first route activation.
    {
      provide: APP_INITIALIZER,
      useFactory: initSession,
      multi: true,
    },

    // Service Worker — enabled in production only; dev mode disables it
    // so hot-reloads are not intercepted.
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
