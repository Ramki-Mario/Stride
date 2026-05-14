import { Routes } from '@angular/router';

export const notificationRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/notifications-page/notifications-page').then((m) => m.NotificationsPageComponent),
  },
];
