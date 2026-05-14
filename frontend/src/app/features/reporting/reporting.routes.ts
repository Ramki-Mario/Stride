import { Routes } from '@angular/router';

export const reportingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/reporting-page/reporting-page').then((m) => m.ReportingPageComponent),
  },
];
