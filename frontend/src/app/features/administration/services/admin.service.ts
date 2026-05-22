import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminUserDto,
  PagedResult,
  InviteUserRequest,
  UpdateUserRoleRequest,
  UserRole,
  UserStatus,
} from '../models/admin-user.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/administration';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly users     = signal<AdminUserDto[]>([]);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  // ── Pagination metadata ───────────────────────────────────────────────────
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly pageSize    = signal(20);
  readonly totalPages  = signal(0);
  readonly hasNext     = signal(false);
  readonly hasPrevious = signal(false);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly isEmpty = computed(() => !this.isLoading() && this.users().length === 0);

  // ── API ───────────────────────────────────────────────────────────────────

  loadUsers(
    page     = 1,
    pageSize = 20,
    search?: string,
    role?:   UserRole | undefined,
    status?: UserStatus | undefined,
  ): void {
    this.isLoading.set(true);
    this.error.set(null);

    let params = new HttpParams()
      .set('page',     page)
      .set('pageSize', pageSize);

    if (search) params = params.set('search', search);
    if (role)   params = params.set('role',   role);
    if (status) params = params.set('status', status);

    this.http.get<PagedResult<AdminUserDto>>(`${this.base}/users`, { params }).subscribe({
      next: result => {
        this.users.set(result.items);
        this.totalCount.set(result.totalCount);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalPages.set(result.totalPages);
        this.hasNext.set(result.hasNextPage);
        this.hasPrevious.set(result.hasPreviousPage);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load users.');
        this.isLoading.set(false);
      },
    });
  }

  inviteUser(request: InviteUserRequest): Observable<unknown> {
    return this.http.post(`${this.base}/users/invite`, request);
  }

  updateUserRole(id: string, request: UpdateUserRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}/role`, request);
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}/deactivate`, {});
  }

  reactivateUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}/reactivate`, {});
  }
}
