import { Routes } from '@angular/router';

export const kitOpsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/kit-catalog-page/kit-catalog-page').then(
        (m) => m.KitCatalogPageComponent
      ),
  },
];
