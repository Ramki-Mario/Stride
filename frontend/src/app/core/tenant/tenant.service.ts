import { Injectable, computed } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class TenantService {
  readonly tenantId = computed(() => this.auth.user()?.tenantId ?? null);

  constructor(private readonly auth: AuthService) {}
}
