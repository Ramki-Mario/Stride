import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ClientSummaryDto,
  ClientDetailDto,
  PagedResult,
  CreateClientRequest,
  UpdateClientRequest,
  ClientStatus,
} from '../models/client.models';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/clients';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly clients   = signal<ClientSummaryDto[]>([]);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  // ── Pagination ────────────────────────────────────────────────────────────
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly pageSize    = signal(20);
  readonly totalPages  = signal(0);
  readonly hasNext     = signal(false);
  readonly hasPrevious = signal(false);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly isEmpty      = computed(() => !this.isLoading() && this.clients().length === 0);
  readonly activeCount  = computed(() => this.clients().filter(c => c.status === 0).length);
  readonly inactiveCount = computed(() => this.clients().filter(c => c.status === 1).length);

  // ── Load ──────────────────────────────────────────────────────────────────

  loadClients(page = 1, pageSize = 20, search?: string, status?: ClientStatus): void {
    this.isLoading.set(true);
    this.error.set(null);

    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined) params = params.set('status', status);

    this.http.get<PagedResult<ClientSummaryDto>>(this.base, { params }).subscribe({
      next: result => {
        this.clients.set(result.items);
        this.totalCount.set(result.totalCount);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalPages.set(result.totalPages);
        this.hasNext.set(result.hasNextPage);
        this.hasPrevious.set(result.hasPreviousPage);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load clients.');
        this.isLoading.set(false);
      },
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  getClientById(id: string): Observable<ClientDetailDto> {
    return this.http.get<ClientDetailDto>(`${this.base}/${id}`);
  }

  createClient(request: CreateClientRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, request);
  }

  updateClient(id: string, request: UpdateClientRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request);
  }

  deactivateClient(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/deactivate`, {});
  }

  reactivateClient(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/reactivate`, {});
  }
}
