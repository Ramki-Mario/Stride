import {
  APP_INITIALIZER,
  ApplicationConfig,
  inject,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { firstValueFrom } from 'rxjs';

import { appRoutes } from './app.routes';
import { correlationInterceptor } from './core/http/correlation.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { AuthService } from './core/auth/auth.service';

/**
 * Checks the BFF session cookie on application boot.
 *
 * Angular waits for all APP_INITIALIZER factories to resolve before rendering
 * any routes, so authGuard's isAuthenticated() short-circuit will fire on the
 * very first navigation — no flicker or redirect to /login for logged-in users.
 *
 * firstValueFrom with { defaultValue: null } swallows 401s silently:
 * an unauthenticated user is a normal state, not an application error.
 * The AuthService._user signal stays null; authGuard redirects to /login.
 */
function initSession() {
  const auth = inject(AuthService);
  return () => firstValueFrom(auth.checkSession(), { defaultValue: null });
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
  ],
};
