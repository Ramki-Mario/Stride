import { Routes } from '@angular/router';

export const administrationRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/administration-page/administration-page').then((m) => m.AdministrationPageComponent),
  },
  {
    path: 'health',
    loadComponent: () =>
      import('./pages/health-page/health-page').then((m) => m.HealthPageComponent),
  },
];
