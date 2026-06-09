// ─── Team summary (GET /bff/teams) ───────────────────────────────────────────

export type TeamStatus = 'Active' | 'Inactive';

export interface TeamSummary {
  id:           string;
  name:         string;
  description:  string | null;
  parentTeamId: string | null;
  parentTeamName: string | null;
  status:       TeamStatus;
  createdAt:    string;   // ISO-8601
}
