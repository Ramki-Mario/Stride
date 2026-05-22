import { Routes } from '@angular/router';

export const invoicingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/invoicing-page/invoicing-page').then(
        (m) => m.InvoicingPageComponent
      ),
  },
];
