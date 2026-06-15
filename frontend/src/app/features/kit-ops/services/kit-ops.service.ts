import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import {
  KitItemSummaryDto,
  KitItemDetailDto,
  KitItemAvailabilityDto,
  PagedResult,
  CreateKitItemRequest,
  UpdateKitItemRequest,
  KitCatalogItemDto,
  MyKitCheckoutDto,
  MyKitReservationDto,
  CheckoutKitItemRequest,
  CreateKitReservationRequest,
  KitCheckoutReportRowDto,
  KitUsageSummaryRowDto,
} from '../models/kit-ops.models';

@Injectable({ providedIn: 'root' })
export class KitOpsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/kit-ops';

  // ── State signals ─────────────────────────────────────────────────────────
  readonly items      = signal<KitItemSummaryDto[]>([]);
  readonly totalCount = signal(0);
  readonly page       = signal(1);
  readonly pageSize   = signal(20);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);

  readonly totalPages  = computed(() => Math.ceil(this.totalCount() / this.pageSize()) || 1);
  readonly hasPrevious = computed(() => this.page() > 1);
  readonly hasNext     = computed(() => this.page() < this.totalPages());
  readonly isEmpty     = computed(() => !this.isLoading() && this.items().length === 0);

  readonly activeCount   = computed(() => this.items().filter(i => i.isActive).length);
  readonly inactiveCount = computed(() => this.items().filter(i => !i.isActive).length);

  // ── List ──────────────────────────────────────────────────────────────────
  loadKitItems(
    page     = 1,
    pageSize = 20,
    search?:   string,
    category?: string,
    isActive?: boolean,
  ): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.page.set(page);
    this.pageSize.set(pageSize);

    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search)            params = params.set('search', search);
    if (category)          params = params.set('category', category);
    if (isActive !== undefined) params = params.set('isActive', String(isActive));

    this.http.get<PagedResult<KitItemSummaryDto>>(`${this.base}/kit-items`, { params })
      .pipe(tap(() => this.isLoading.set(false)))
      .subscribe({
        next: result => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('Failed to load kit items. Please try again.');
          this.isLoading.set(false);
        },
      });
  }

  // ── Single item ───────────────────────────────────────────────────────────
  getKitItemById(id: string): Observable<KitItemDetailDto> {
    return this.http.get<KitItemDetailDto>(`${this.base}/kit-items/${id}`);
  }

  getKitItemAvailability(id: string): Observable<KitItemAvailabilityDto> {
    return this.http.get<KitItemAvailabilityDto>(`${this.base}/kit-items/${id}/availability`);
  }

  // ── Mutations ─────────────────────────────────────────────────────────────
  createKitItem(body: CreateKitItemRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/kit-items`, body);
  }

  updateKitItem(id: string, body: UpdateKitItemRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/kit-items/${id}`, body);
  }

  deactivateKitItem(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/kit-items/${id}/deactivate`, {});
  }

  reactivateKitItem(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/kit-items/${id}/reactivate`, {});
  }

  // ── Field user ────────────────────────────────────────────────────────────
  getCatalog(): Observable<KitCatalogItemDto[]> {
    return this.http.get<KitCatalogItemDto[]>(`${this.base}/catalog`);
  }

  getMyCheckouts(): Observable<MyKitCheckoutDto[]> {
    return this.http.get<MyKitCheckoutDto[]>(`${this.base}/checkouts/my`);
  }

  checkout(body: CheckoutKitItemRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/checkouts`, body);
  }

  returnCheckout(checkoutId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/checkouts/${checkoutId}/return`, {});
  }

  getMyReservations(): Observable<MyKitReservationDto[]> {
    return this.http.get<MyKitReservationDto[]>(`${this.base}/reservations/my`);
  }

  createReservation(body: CreateKitReservationRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/reservations`, body);
  }

  cancelReservation(reservationId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/reservations/${reservationId}/cancel`, {});
  }

  // ── Reports ───────────────────────────────────────────────────────────────
  getCheckoutHistory(from?: string, to?: string, kitItemId?: string): Observable<KitCheckoutReportRowDto[]> {
    let params = this.buildReportParams(from, to);
    if (kitItemId) params = params.set('kitItemId', kitItemId);
    return this.http.get<KitCheckoutReportRowDto[]>(`${this.base}/reports/checkout-history`, { params });
  }

  exportCheckoutHistory(from?: string, to?: string): Observable<Blob> {
    let params = this.buildReportParams(from, to);
    return this.http.get(`${this.base}/reports/checkout-history/export`,
      { params, responseType: 'blob' });
  }

  getUsageSummary(from?: string, to?: string): Observable<KitUsageSummaryRowDto[]> {
    const params = this.buildReportParams(from, to);
    return this.http.get<KitUsageSummaryRowDto[]>(`${this.base}/reports/usage-summary`, { params });
  }

  exportUsageSummary(from?: string, to?: string): Observable<Blob> {
    const params = this.buildReportParams(from, to);
    return this.http.get(`${this.base}/reports/usage-summary/export`,
      { params, responseType: 'blob' });
  }

  private buildReportParams(from?: string, to?: string) {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to)   params = params.set('to', to);
    return params;
  }
}
