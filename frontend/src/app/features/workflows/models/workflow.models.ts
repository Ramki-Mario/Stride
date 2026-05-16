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

// ─── Step instance ────────────────────────────────────────────────────────────

export type StepInstanceStatus = 'Pending' | 'InProgress' | 'Completed' | 'Failed' | 'Skipped';

export interface StepInstance {
  id: string;
  stepDefinitionId: string;
  name: string;
  description: string | null;
  order: number;
  isRequired: boolean;
  status: StepInstanceStatus;
  assignedTo: string | null;    // userId GUID
  completedAt: string | null;
  failureReason: string | null;
}

export const STEP_INSTANCE_STATUS_CONFIG: Record<
  StepInstanceStatus,
  { label: string; cssClass: string; icon: string }
> = {
  Pending:    { label: 'Pending',     cssClass: 'ssi-pending',    icon: 'pi-clock'         },
  InProgress: { label: 'In Progress', cssClass: 'ssi-inprogress', icon: 'pi-spin pi-spinner' },
  Completed:  { label: 'Completed',   cssClass: 'ssi-completed',  icon: 'pi-check-circle'  },
  Failed:     { label: 'Failed',      cssClass: 'ssi-failed',     icon: 'pi-times-circle'  },
  Skipped:    { label: 'Skipped',     cssClass: 'ssi-skipped',    icon: 'pi-minus-circle'  },
};

// ─── Instance detail (GET /bff/workflows/instances/:id) ──────────────────────

export interface WorkflowInstanceDetail {
  id: string;
  workflowDefinitionId: string;
  workflowName: string;
  status: WorkflowStatus;
  totalSteps: number;
  completedSteps: number;
  startedAt: string;
  completedAt: string | null;
  steps: StepInstance[];
}

// ─── Step action types ────────────────────────────────────────────────────────

export type StepAction = 'assign' | 'complete' | 'fail' | 'skip';

// ─── Form request payloads ────────────────────────────────────────────────────

export interface StepRequest {
  name: string;
  description: string | null;
  isRequired: boolean;
}

export interface CreateWorkflowRequest {
  name: string;
  description: string | null;
  steps: StepRequest[];
}

export interface UpdateWorkflowRequest {
  name: string;
  description: string | null;
}

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
