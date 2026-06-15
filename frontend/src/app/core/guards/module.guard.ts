import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ModuleService } from '../services/module.service';

export const moduleGuard: CanActivateFn = (route) => {
  const moduleName: string | undefined = route.data?.['module'];
  if (!moduleName) return true;

  const moduleService = inject(ModuleService);
  if (moduleService.isEnabled(moduleName)) return true;

  inject(Router).navigate(['/dashboard']);
  return false;
};
