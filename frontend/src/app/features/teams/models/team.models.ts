// ─── Team status ─────────────────────────────────────────────────────────────
// Backend: int (0 = Active, 1 = Inactive) — serialised as a number in JSON.

export type TeamStatus = 0 | 1;   // 0 = Active, 1 = Inactive

// ─── Team summary (GET /bff/teams — paged list) ──────────────────────────────

export interface TeamSummaryDto {
  id:             string;
  name:           string;
  description:    string | null;
  parentTeamId:   string | null;
  parentTeamName: string | null;
  status:         TeamStatus;
  statusLabel:    string;          // "Active" | "Inactive"
  createdAt:      string;          // ISO-8601
}

// ─── Team detail (GET /bff/teams/:id) ────────────────────────────────────────

export interface TeamDetailDto {
  id:             string;
  name:           string;
  description:    string | null;
  parentTeamId:   string | null;
  parentTeamName: string | null;
  status:         TeamStatus;
  statusLabel:    string;
  createdAt:      string;
  updatedAt:      string;
}

// ─── Paged result wrapper (same shape as other modules) ──────────────────────

export interface PagedResult<T> {
  items:           T[];
  totalCount:      number;
  page:            number;
  pageSize:        number;
  totalPages:      number;
  hasNextPage:     boolean;
  hasPreviousPage: boolean;
}

// ─── Request types ────────────────────────────────────────────────────────────

export interface CreateTeamRequest {
  name:         string;
  description:  string | null;
  parentTeamId: string | null;
}

export interface UpdateTeamRequest {
  name:         string;
  description:  string | null;
  parentTeamId: string | null;
}

// ─── Lightweight model used by the team picker in other features ─────────────
// (kept for backwards compat — workflow-detail-page imports TeamSummary)

export interface TeamSummary {
  id:             string;
  name:           string;
  description:    string | null;
  parentTeamId:   string | null;
  parentTeamName: string | null;
  status:         TeamStatus;
  statusLabel:    string;
  createdAt:      string;
}
