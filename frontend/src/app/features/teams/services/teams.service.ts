import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TeamSummary } from '../models/team.models';

/**
 * Calls the BFF endpoints for the Teams module.
 *
 * BFF route → Host route mapping:
 *   GET /bff/teams → GET /api/teams
 *
 * Note: The /bff/teams endpoint is provided by the Teams module (EP-055/US-168).
 * This service depends on US-168 being deployed.
 */
@Injectable({ providedIn: 'root' })
export class TeamsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/teams';

  /**
   * Returns all active teams for the current tenant, optionally filtered.
   * @param search Optional name/description search string.
   * @param status Optional status filter (defaults to 'Active' teams).
   */
  getTeams(search?: string, status: string = 'Active'): Observable<TeamSummary[]> {
    let params = new HttpParams().set('status', status).set('pageSize', '200');
    if (search) params = params.set('search', search);
    return this.http.get<TeamSummary[]>(this.base, { params });
  }
}
