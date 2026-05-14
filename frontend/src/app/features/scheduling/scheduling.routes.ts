import { Routes } from '@angular/router';

export const schedulingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/scheduling-page/scheduling-page').then((m) => m.SchedulingPageComponent),
  },
];
