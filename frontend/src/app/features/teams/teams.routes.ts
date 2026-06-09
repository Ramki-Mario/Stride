import { Routes } from '@angular/router';

export const teamRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/teams-page/teams-page').then(
        (m) => m.TeamsPageComponent
      ),
  },
];
