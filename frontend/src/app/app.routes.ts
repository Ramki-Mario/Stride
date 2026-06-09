import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/pages/login-page/login-page').then(
        (m) => m.LoginPageComponent
      ),
  },
  {
    // Public route — no authGuard. New customers complete the 4-step wizard here.
    path: 'onboarding',
    loadComponent: () =>
      import('./features/onboarding/pages/onboarding-page/onboarding-page').then(
        (m) => m.OnboardingPageComponent
      ),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell').then((m) => m.ShellComponent),
    children: [
      {
        // Dashboard KPI overview (US-062) — lives in features/dashboard per coding conventions.
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard-page/dashboard-page').then(
            (m) => m.DashboardPageComponent
          ),
      },
      {
        path: 'workflows',
        loadChildren: () =>
          import('./features/workflows/workflows.routes').then(
            (m) => m.workflowRoutes
          ),
      },
      {
        path: 'scheduling',
        loadChildren: () =>
          import('./features/scheduling/scheduling.routes').then(
            (m) => m.schedulingRoutes
          ),
      },
      {
        path: 'reporting',
        loadChildren: () =>
          import('./features/reporting/reporting.routes').then(
            (m) => m.reportingRoutes
          ),
      },
      {
        path: 'notifications',
        loadChildren: () =>
          import('./features/notifications/notifications.routes').then(
            (m) => m.notificationRoutes
          ),
      },
      {
        path: 'administration',
        loadChildren: () =>
          import('./features/administration/administration.routes').then(
            (m) => m.administrationRoutes
          ),
      },
      {
        path: 'clients',
        loadChildren: () =>
          import('./features/clients/clients.routes').then(
            (m) => m.clientRoutes
          ),
      },
      {
        path: 'invoicing',
        loadChildren: () =>
          import('./features/invoicing/invoicing.routes').then(
            (m) => m.invoicingRoutes
          ),
      },
      {
        path: 'teams',
        loadChildren: () =>
          import('./features/teams/teams.routes').then(
            (m) => m.teamRoutes
          ),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: '' },
];
