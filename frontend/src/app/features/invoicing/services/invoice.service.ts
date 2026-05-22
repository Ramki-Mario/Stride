import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  InvoiceSummaryDto,
  InvoiceDetailDto,
  PagedResult,
  GenerateInvoiceRequest,
  InvoiceStatus,
} from '../models/invoice.models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/invoicing/invoices';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly invoices   = signal<InvoiceSummaryDto[]>([]);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);

  // ── Pagination ────────────────────────────────────────────────────────────
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly pageSize    = signal(20);
  readonly totalPages  = signal(0);
  readonly hasNext     = signal(false);
  readonly hasPrevious = signal(false);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly isEmpty = computed(() => !this.isLoading() && this.invoices().length === 0);

  // ── KPI summaries (computed from loaded page) ─────────────────────────────
  readonly totalRevenue = computed(() =>
    this.invoices().filter(i => i.status === 2).reduce((s, i) => s + i.totalAmount, 0));

  readonly pendingCount = computed(() =>
    this.invoices().filter(i => i.status === 1).length);

  readonly draftCount = computed(() =>
    this.invoices().filter(i => i.status === 0).length);

  // ── Load ──────────────────────────────────────────────────────────────────

  loadInvoices(page = 1, pageSize = 20, search?: string, status?: InvoiceStatus): void {
    this.isLoading.set(true);
    this.error.set(null);

    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined) params = params.set('status', status);

    this.http.get<PagedResult<InvoiceSummaryDto>>(this.base, { params }).subscribe({
      next: result => {
        this.invoices.set(result.items);
        this.totalCount.set(result.totalCount);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalPages.set(result.totalPages);
        this.hasNext.set(result.hasNextPage);
        this.hasPrevious.set(result.hasPreviousPage);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load invoices.');
        this.isLoading.set(false);
      },
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  generateInvoice(request: GenerateInvoiceRequest): Observable<unknown> {
    return this.http.post(this.base, request);
  }

  getInvoiceById(id: string): Observable<InvoiceDetailDto> {
    return this.http.get<InvoiceDetailDto>(`${this.base}/${id}`);
  }

  sendInvoice(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/send`, {});
  }

  markPaid(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/paid`, {});
  }

  voidInvoice(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/void`, {});
  }
}
