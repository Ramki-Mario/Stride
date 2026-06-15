import { Injectable, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class ModuleService {
  private readonly auth = inject(AuthService);

  /**
   * Returns true when the named module is available for the current tenant.
   * An empty `enabledModules` array means all modules are enabled (default for
   * existing tenants before the per-tenant gating feature was shipped).
   */
  isEnabled(moduleName: string): boolean {
    const modules = this.auth.user()?.enabledModules ?? [];
    return modules.length === 0 || modules.includes(moduleName);
  }
}
