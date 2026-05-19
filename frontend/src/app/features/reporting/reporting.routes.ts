import { Routes } from '@angular/router';

export const reportingRoutes: Routes = [
  {
    // /reporting  → Reporting page (report list, generate, export CSV) — US-063
    path: '',
    loadComponent: () =>
      import('./pages/reporting-page/reporting-page').then(
        (m) => m.ReportingPageComponent,
      ),
  },
];
