import { Routes } from '@angular/router';

export const reportingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/dashboard-page/dashboard-page').then(
        (m) => m.DashboardPageComponent,
      ),
  },
];
