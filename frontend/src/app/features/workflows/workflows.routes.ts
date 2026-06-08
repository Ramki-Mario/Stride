import { Routes } from '@angular/router';

export const workflowRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/workflows-page/workflows-page').then((m) => m.WorkflowsPageComponent),
  },
  {
    // 'my-tasks', 'instances', and 'new' MUST appear before ':id' — static before dynamic
    path: 'my-tasks',
    loadComponent: () =>
      import('./pages/my-tasks-page/my-tasks-page').then(
        (m) => m.MyTasksPageComponent,
      ),
  },
  {
    path: 'instances',
    loadComponent: () =>
      import('./pages/workflow-instances-page/workflow-instances-page').then(
        (m) => m.WorkflowInstancesPageComponent,
      ),
  },
  {
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
