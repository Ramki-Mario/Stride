import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * Intercepts HTTP errors and navigates to the appropriate error page.
 *
 * 401 handling — suppressed for BFF auth flow endpoints:
 *   /bff/auth/login  → LoginPageComponent handles the 401 inline (wrong credentials)
 *   /bff/auth/me     → APP_INITIALIZER + authGuard handle the 401 (not logged in)
 * For all other 401s (e.g. session expired mid-session on an API call) we
 * redirect to /login so the user can re-authenticate.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401) {
        const isAuthFlowEndpoint = /\/bff\/auth\/(login|me)$/.test(req.url);
        if (!isAuthFlowEndpoint) router.navigate(['/login']);
      }
      if (error.status === 403) router.navigate(['/forbidden']);
      return throwError(() => error);
    })
  );
};
