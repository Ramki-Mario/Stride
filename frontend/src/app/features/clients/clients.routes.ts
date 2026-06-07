import { Routes } from '@angular/router';

export const clientRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/clients-page/clients-page').then(
        (m) => m.ClientsPageComponent
      ),
  },
];
