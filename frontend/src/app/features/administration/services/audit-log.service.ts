import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { AuditLogEntry, AuditLogPagedResult } from '../models/audit-log.models';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/administration/audit-log';

  readonly entries    = signal<AuditLogEntry[]>([]);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page       = signal(1);
  readonly pageSize   = signal(50);
  readonly totalPages = signal(0);
  readonly hasNext    = signal(false);
  readonly hasPrevious = signal(false);

  load(
    page     = 1,
    pageSize = 50,
    from?:    string,
    to?:      string,
    action?:  string,
  ): void {
    this.isLoading.set(true);
    this.error.set(null);

    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (from)   params = params.set('from', from);
    if (to)     params = params.set('to', to);
    if (action) params = params.set('action', action);

    this.http.get<AuditLogPagedResult>(this.base, { params }).subscribe({
      next: (res) => {
        this.entries.set(res.items);
        this.totalCount.set(res.totalCount);
        this.page.set(res.page);
        this.pageSize.set(res.pageSize);
        this.totalPages.set(res.totalPages);
        this.hasNext.set(res.hasNext);
        this.hasPrevious.set(res.hasPrevious);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Failed to load audit log.');
        this.isLoading.set(false);
      },
    });
  }
}
