import { Routes } from '@angular/router';

export const invoicingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/invoicing-page/invoicing-page').then(
        (m) => m.InvoicingPageComponent
      ),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/invoice-detail-page/invoice-detail-page').then(
        (m) => m.InvoiceDetailPageComponent
      ),
  },
];
