import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  TeamSummaryDto,
  TeamDetailDto,
  PagedResult,
  CreateTeamRequest,
  UpdateTeamRequest,
  TeamStatus,
  TeamSummary,
} from '../models/team.models';

/**
 * Calls the BFF endpoints for the Teams module.
 *
 * BFF route → Host route mapping:
 *   GET  /bff/teams       → GET  /api/teams
 *   POST /bff/teams       → POST /api/teams
 *   GET  /bff/teams/:id   → GET  /api/teams/:id
 *   PUT  /bff/teams/:id   → PUT  /api/teams/:id
 *   PUT  /bff/teams/:id/deactivate → PUT /api/teams/:id/deactivate
 *   PUT  /bff/teams/:id/reactivate → PUT /api/teams/:id/reactivate
 *
 * Note: The /bff/teams endpoints are provided by the Teams module (EP-055/US-168).
 */
@Injectable({ providedIn: 'root' })
export class TeamsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/teams';

  // ── Server state ─────────────────────────────────────────────────────────
  readonly teams     = signal<TeamSummaryDto[]>([]);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);

  // ── Pagination ────────────────────────────────────────────────────────────
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly pageSize    = signal(20);
  readonly totalPages  = signal(0);
  readonly hasNext     = signal(false);
  readonly hasPrevious = signal(false);

  // ── Derived ────────────────────────────────────────────────────────────────
  readonly isEmpty       = computed(() => !this.isLoading() && this.teams().length === 0);
  readonly activeCount   = computed(() => this.teams().filter(t => t.status === 0).length);
  readonly inactiveCount = computed(() => this.teams().filter(t => t.status === 1).length);

  // ── Load (populates signals for the list page) ────────────────────────────

  loadTeams(page = 1, pageSize = 20, search?: string, status?: TeamStatus): void {
    this.isLoading.set(true);
    this.error.set(null);

    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search)              params = params.set('search', search);
    if (status !== undefined) params = params.set('status', status);

    this.http.get<PagedResult<TeamSummaryDto>>(this.base, { params }).subscribe({
      next: result => {
        this.teams.set(result.items);
        this.totalCount.set(result.totalCount);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalPages.set(result.totalPages);
        this.hasNext.set(result.hasNextPage);
        this.hasPrevious.set(result.hasPreviousPage);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load teams.');
        this.isLoading.set(false);
      },
    });
  }

  // ── Lightweight fetch for pickers (e.g. workflow-detail team picker) ──────
  /**
   * Returns a flat list of active teams — used by team pickers in other pages.
   * Does NOT mutate the paginated signals used by the teams management page.
   */
  getTeams(search?: string, status: TeamStatus = 0): Observable<TeamSummary[]> {
    let params = new HttpParams().set('status', status).set('pageSize', '200');
    if (search) params = params.set('search', search);
    // Backend returns PagedResult<TeamSummaryDto> — extract the items array for pickers.
    return this.http.get<PagedResult<TeamSummary>>(this.base, { params }).pipe(
      map(result => result.items),
    );
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  getTeamById(id: string): Observable<TeamDetailDto> {
    return this.http.get<TeamDetailDto>(`${this.base}/${id}`);
  }

  createTeam(request: CreateTeamRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, request);
  }

  updateTeam(id: string, request: UpdateTeamRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, request);
  }

  deactivateTeam(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/deactivate`, {});
  }

  reactivateTeam(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/reactivate`, {});
  }
}
