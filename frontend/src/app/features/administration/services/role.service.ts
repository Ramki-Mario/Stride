import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PermissionDto,
  RoleDto,
  RoleDetailDto,
  CreateRoleRequest,
  UpdateRoleRequest,
} from '../models/role.models';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/identity/roles';
  private readonly permBase = '/bff/identity/permissions';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly roles      = signal<RoleDto[]>([]);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly isEmpty      = computed(() => !this.isLoading() && this.roles().length === 0);
  readonly totalCount   = computed(() => this.roles().length);
  readonly customCount  = computed(() => this.roles().filter(r => !(r as any).isSystemRole).length);

  // ── Load list ─────────────────────────────────────────────────────────────
  loadRoles(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.get<RoleDto[]>(this.base).subscribe({
      next:  roles => { this.roles.set(roles); this.isLoading.set(false); },
      error: ()    => { this.error.set('Failed to load roles.'); this.isLoading.set(false); },
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────
  getPermissions(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>(this.permBase);
  }

  getRole(id: string): Observable<RoleDetailDto> {
    return this.http.get<RoleDetailDto>(`${this.base}/${id}`);
  }

  createRole(request: CreateRoleRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, request);
  }

  updateRole(id: string, request: UpdateRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
