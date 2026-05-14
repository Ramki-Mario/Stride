import { Routes } from '@angular/router';

export const workflowRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/workflows-page/workflows-page').then((m) => m.WorkflowsPageComponent),
  },
];
