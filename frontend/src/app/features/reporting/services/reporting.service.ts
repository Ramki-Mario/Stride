import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardKpiDto, WorkflowTrendDto, ReportSummaryDto } from '../models/reporting.models';

/**
 * Calls the BFF endpoints for reporting/dashboard data.
 *
 * BFF route → Host route mapping:
 *   GET /bff/reporting/dashboard/kpis     → GET /api/reporting/dashboard/kpis
 *   GET /bff/reporting/dashboard/trends   → GET /api/reporting/dashboard/trends?days=N
 *   GET /bff/reporting/reports            → GET /api/reporting/reports
 *
 * Note: BFF reporting proxy is deferred to Phase 6 (Issue #109). These URLs are
 * the intended BFF surface — the service will fail gracefully until the proxy is wired.
 */
@Injectable({ providedIn: 'root' })
export class ReportingService {
  private readonly http = inject(HttpClient);

  private readonly base = '/bff/reporting';

  /** Dashboard KPI snapshot — counts for the header row. */
  getDashboardKpis(): Observable<DashboardKpiDto> {
    return this.http.get<DashboardKpiDto>(`${this.base}/dashboard/kpis`);
  }

  /**
   * Workflow activity trend time series.
   * @param days lookback window in days (1–365, default 30)
   */
  getWorkflowTrends(days = 30): Observable<WorkflowTrendDto[]> {
    return this.http.get<WorkflowTrendDto[]>(
      `${this.base}/dashboard/trends?days=${days}`,
    );
  }

  /** List all saved reports for the current tenant, newest first. */
  getReports(): Observable<ReportSummaryDto[]> {
    return this.http.get<ReportSummaryDto[]>(`${this.base}/reports`);
  }
}
