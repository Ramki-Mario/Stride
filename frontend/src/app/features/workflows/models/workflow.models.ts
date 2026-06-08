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

// ─── Billable items ───────────────────────────────────────────────────────────

export type BillableUnit = 'Hours' | 'Each' | 'Day' | 'Fixed';

export const BILLABLE_UNIT_LABELS: Record<BillableUnit, string> = {
  Hours: 'Hours',
  Each:  'Each',
  Day:   'Day',
  Fixed: 'Fixed fee',
};

export interface BillableItemDto {
  id:          string;
  description: string;
  quantity:    number;
  unitPrice:   number;
  unit:        BillableUnit;
  lineTotal:   number;
}

export interface BillableItemInput {
  description: string;
  quantity:    number;
  unitPrice:   number;
  unit:        BillableUnit;
}

export interface FieldValueInput {
  stepFieldDefinitionId: string;
  value:                 string;
}

// ─── Step instance ────────────────────────────────────────────────────────────

export type StepInstanceStatus = 'Pending' | 'InProgress' | 'Completed' | 'Failed' | 'Skipped';

export interface StepInstance {
  id: string;
  stepDefinitionId: string;
  /** Matches backend StepInstanceDto.StepName → JSON "stepName". */
  stepName:       string;
  order:          number;
  isRequired:     boolean;
  status:         StepInstanceStatus;
  /** Matches backend StepInstanceDto.AssigneeId → JSON "assigneeId". */
  assigneeId:     string | null;
  assignedAt:     string | null;
  /** Cross-module FK to Identity.Role — bare Guid, no navigation. */
  requiredRoleId: string | null;
  completedAt:    string | null;
  dueAt:          string | null;
  failureReason:  string | null;
  billableItems:    BillableItemDto[];
  billableSubtotal: number;
  fields:           FieldDefinition[];
  fieldValues:      StepFieldValueDto[];
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
  /** Matches backend WorkflowInstanceDto.CreatedAt → JSON "createdAt" (instance creation = start time). */
  createdAt: string;
  completedAt: string | null;
  steps: StepInstance[];
  billableTotal: number;
  // Note: totalSteps and completedSteps are not in the backend DTO.
  // Compute them from steps.length and steps.filter(s => s.status === 'Completed').length.
}

// ─── Step action types ────────────────────────────────────────────────────────

export type StepAction = 'assign' | 'claim' | 'complete' | 'fail' | 'skip';

// ─── Step field definitions ───────────────────────────────────────────────────

export type FieldType =
  | 'Text'
  | 'Number'
  | 'Currency'
  | 'Hours'
  | 'Dropdown'
  | 'Date'
  | 'Boolean'
  | 'LongText';

export const FIELD_TYPE_LABELS: Record<FieldType, string> = {
  Text:     'Text',
  Number:   'Number',
  Currency: 'Currency amount',
  Hours:    'Hours worked',
  Dropdown: 'Dropdown (select)',
  Date:     'Date',
  Boolean:  'Yes / No',
  LongText: 'Long text (notes)',
};

export const FIELD_TYPES: FieldType[] = [
  'Text', 'Number', 'Currency', 'Hours', 'Dropdown', 'Date', 'Boolean', 'LongText',
];

/** Read model — returned by GET /bff/workflows/definitions/:id for each step. */
export interface FieldDefinition {
  id:             string;
  label:          string;
  fieldType:      FieldType;
  isRequired:     boolean;
  displayOrder:   number;
  helpText:       string | null;
  dropdownOptions: string[];
}

export interface StepFieldValueDto {
  id:                    string;
  stepFieldDefinitionId: string;
  value:                 string;
}

/** Write model — submitted in the create-workflow request body. */
export interface FieldDefinitionRequest {
  label:          string;
  fieldType:      FieldType;
  isRequired:     boolean;
  helpText:       string | null;
  dropdownOptions: string[] | null;
}

// ─── Form request payloads ────────────────────────────────────────────────────

export interface StepRequest {
  name:             string;
  description:      string | null;
  isRequired:       boolean;
  requiredRoleId:   string | null;
  dueOffsetHours:   number | null;
  fieldDefinitions: FieldDefinitionRequest[];
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

// ─── My Tasks (GET /bff/workflows/my-tasks) ──────────────────────────────────

export interface MyTask {
  stepInstanceId:    string;
  stepName:          string;
  stepStatus:        StepInstanceStatus;
  assignedAt:        string | null;   // ISO-8601
  workflowInstanceId: string;
  workflowName:      string;
  workflowStatus:    WorkflowStatus;
  clientName:        string | null;
}

// ─── Definition detail (GET /bff/workflows/definitions/:id) ──────────────────

export interface RoleDto {
  id:          string;
  name:        string;
  description: string;
}

export interface StepDefinition {
  id:              string;
  name:            string;
  description:     string | null;
  order:           number;
  isRequired:      boolean;
  requiredRoleId:  string | null;
  dueOffsetHours:  number | null;
  fields:          FieldDefinition[];
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
