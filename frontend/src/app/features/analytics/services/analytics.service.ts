import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AnalyticsWorkflowDefinitionDto,
  CompletionTimeAnalyticsDto,
  RevenueAnalyticsDto,
  RoleDto,
  TeamMemberPerformanceDto,
} from '../models/analytics.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/analytics';

  getWorkflowDefinitions(): Observable<AnalyticsWorkflowDefinitionDto[]> {
    return this.http.get<AnalyticsWorkflowDefinitionDto[]>(`${this.base}/workflow-definitions`);
  }

  getCompletionTimes(
    fromDate: string,
    toDate: string,
    workflowDefinitionId?: string,
  ): Observable<CompletionTimeAnalyticsDto> {
    let params = new HttpParams()
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    if (workflowDefinitionId) {
      params = params.set('workflowDefinitionId', workflowDefinitionId);
    }

    return this.http.get<CompletionTimeAnalyticsDto>(
      `${this.base}/completion-times`, { params });
  }

  getExportUrl(fromDate: string, toDate: string, workflowDefinitionId?: string): string {
    let url = `${this.base}/completion-times/export?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}`;
    if (workflowDefinitionId) {
      url += `&workflowDefinitionId=${encodeURIComponent(workflowDefinitionId)}`;
    }
    return url;
  }

  getRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>('/bff/identity/roles');
  }

  getTeamPerformance(
    fromDate: string,
    toDate: string,
    roleId?: string,
  ): Observable<TeamMemberPerformanceDto[]> {
    let params = new HttpParams()
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    if (roleId) {
      params = params.set('roleId', roleId);
    }

    return this.http.get<TeamMemberPerformanceDto[]>(
      `${this.base}/team/performance`, { params });
  }

  getTeamPerformanceExportUrl(fromDate: string, toDate: string, roleId?: string): string {
    let url = `${this.base}/team/performance/export?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}`;
    if (roleId) {
      url += `&roleId=${encodeURIComponent(roleId)}`;
    }
    return url;
  }

  getRevenueAnalytics(fromDate: string, toDate: string): Observable<RevenueAnalyticsDto> {
    const params = new HttpParams()
      .set('fromDate', fromDate)
      .set('toDate', toDate);
    return this.http.get<RevenueAnalyticsDto>(`${this.base}/revenue`, { params });
  }

  getRevenueExportUrl(fromDate: string, toDate: string): string {
    return `${this.base}/revenue/export?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}`;
  }
}
