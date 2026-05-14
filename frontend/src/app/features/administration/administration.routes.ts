import { Routes } from '@angular/router';

export const administrationRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/administration-page/administration-page').then((m) => m.AdministrationPageComponent),
  },
];
