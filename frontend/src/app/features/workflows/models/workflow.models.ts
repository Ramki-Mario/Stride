// ─── Enums ───────────────────────────────────────────────────────────────────

export type WorkflowStatus =
  | 'Draft'
  | 'Active'
  | 'Running'
  | 'Paused'
  | 'Completed'
  | 'Cancelled'
  | 'Failed'
  | 'Archived';

// ─── Definition summary (GET /bff/workflows/definitions) ─────────────────────

export interface WorkflowDefinitionSummary {
  id: string;
  name: string;
  description: string | null;
  status: WorkflowStatus;
  stepCount: number;
  createdAt: string;   // ISO-8601
  updatedAt: string;   // ISO-8601
}

// ─── Instance summary (GET /bff/workflows/instances) ────────────────────────

export interface WorkflowInstanceSummary {
  id: string;
  workflowDefinitionId: string;
  workflowName: string;
  status: WorkflowStatus;
  totalSteps: number;
  completedSteps: number;
  createdAt: string;
  completedAt: string | null;
}

// ─── UI helpers ──────────────────────────────────────────────────────────────

export interface StatusConfig {
  label: string;
  cssClass: string;   // applied to the badge element
}

export const STATUS_CONFIG: Record<WorkflowStatus, StatusConfig> = {
  Draft:     { label: 'Draft',     cssClass: 'badge-draft'     },
  Active:    { label: 'Active',    cssClass: 'badge-active'    },
  Running:   { label: 'Running',   cssClass: 'badge-running'   },
  Paused:    { label: 'Paused',    cssClass: 'badge-paused'    },
  Completed: { label: 'Completed', cssClass: 'badge-completed' },
  Cancelled: { label: 'Cancelled', cssClass: 'badge-cancelled' },
  Failed:    { label: 'Failed',    cssClass: 'badge-failed'    },
  Archived:  { label: 'Archived',  cssClass: 'badge-archived'  },
};

export type SortKey = 'name' | 'status' | 'stepCount' | 'updatedAt' | 'createdAt';

// ─── Definition detail (GET /bff/workflows/definitions/:id) ──────────────────

export interface StepDefinition {
  id: string;
  name: string;
  description: string | null;
  order: number;
  isRequired: boolean;
}

export interface WorkflowDefinitionDetail {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  status: WorkflowStatus;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  steps: StepDefinition[];
}
