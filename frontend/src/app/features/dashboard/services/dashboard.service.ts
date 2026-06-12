import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardAlertSummary } from '../models/dashboard-alert.models';
import { TeamWorkloadItem } from '../models/dashboard-workload.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/reporting';

  getAlerts(): Observable<DashboardAlertSummary> {
    return this.http.get<DashboardAlertSummary>(`${this.base}/dashboard/alerts`);
  }

  getTeamWorkload(): Observable<TeamWorkloadItem[]> {
    return this.http.get<TeamWorkloadItem[]>(`${this.base}/dashboard/team-workload`);
  }
}
