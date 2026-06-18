import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
  plan: string;
  isActive: boolean;
  seatCount: number;
  createdAt: string;
  lastActivityAt: string | null;
}

export interface TenantUserSummary {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
  roles: string[];
}

export interface TenantDetail extends TenantSummary {
  users: TenantUserSummary[];
}

export interface PlatformStats {
  totalTenants: number;
  activeTenants: number;
  totalUsers: number;
  newTenantsThisWeek: number;
}

@Injectable({ providedIn: 'root' })
export class PlatformAdminService {
  constructor(private readonly http: HttpClient) {}

  getTenants() {
    return this.http.get<TenantSummary[]>('/bff/platform/tenants');
  }

  getTenant(id: string) {
    return this.http.get<TenantDetail>(`/bff/platform/tenants/${id}`);
  }

  getStats() {
    return this.http.get<PlatformStats>('/bff/platform/stats');
  }
}
