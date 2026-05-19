import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DashboardKpiDto,
  WorkflowTrendDto,
  ReportSummaryDto,
  GenerateReportRequest,
} from '../models/reporting.models';

@Injectable({ providedIn: 'root' })
export class ReportingService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/reporting';

  getDashboardKpis(): Observable<DashboardKpiDto> {
    return this.http.get<DashboardKpiDto>(`${this.base}/dashboard/kpis`);
  }

  getWorkflowTrends(days = 30): Observable<WorkflowTrendDto[]> {
    return this.http.get<WorkflowTrendDto[]>(`${this.base}/dashboard/trends?days=${days}`);
  }

  getReports(): Observable<ReportSummaryDto[]> {
    return this.http.get<ReportSummaryDto[]>(`${this.base}/reports`);
  }

  generateReport(req: GenerateReportRequest): Observable<{ reportId: string }> {
    return this.http.post<{ reportId: string }>(`${this.base}/reports/generate`, req);
  }

  exportReportCsv(reportId: string): Observable<Blob> {
    return this.http.get(`${this.base}/reports/${reportId}/export`, {
      responseType: 'blob',
    });
  }
}
