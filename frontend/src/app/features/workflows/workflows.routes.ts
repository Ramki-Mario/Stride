import { Routes } from '@angular/router';

export const workflowRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/workflows-page/workflows-page').then((m) => m.WorkflowsPageComponent),
  },
  {
    // 'new' MUST appear before ':id' — static segments match before dynamic ones
    path: 'new',
    loadComponent: () =>
      import('./pages/workflow-form-page/workflow-form-page').then(
        (m) => m.WorkflowFormPageComponent,
      ),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/workflow-detail-page/workflow-detail-page').then(
        (m) => m.WorkflowDetailPageComponent,
      ),
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./pages/workflow-form-page/workflow-form-page').then(
        (m) => m.WorkflowFormPageComponent,
      ),
  },
];
