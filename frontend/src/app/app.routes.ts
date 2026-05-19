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
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell').then((m) => m.ShellComponent),
    children: [
      {
        // Dashboard is the KPI overview powered by Reporting data (US-062).
        // The features/dashboard stub is superseded — load the real component.
        path: 'dashboard',
        loadComponent: () =>
          import('./features/reporting/pages/dashboard-page/dashboard-page').then(
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
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: '' },
];
