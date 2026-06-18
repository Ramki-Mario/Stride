import { Routes } from '@angular/router';
import { superAdminGuard } from '../../core/guards/super-admin.guard';

export const platformAdminRoutes: Routes = [
  {
    path: '',
    canActivate: [superAdminGuard],
    loadComponent: () =>
      import('./pages/tenant-list-page/tenant-list-page').then(
        (m) => m.TenantListPageComponent
      ),
  },
  {
    path: 'tenants/:id',
    canActivate: [superAdminGuard],
    loadComponent: () =>
      import('./pages/tenant-detail-page/tenant-detail-page').then(
        (m) => m.TenantDetailPageComponent
      ),
  },
];
