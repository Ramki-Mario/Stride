export interface AnalyticsWorkflowDefinitionDto {
  id:   string;
  name: string;
}

export interface WorkflowCompletionTimeDto {
  definitionId:       string;
  definitionName:     string;
  avgDurationMinutes: number;
  instanceCount:      number;
}

export interface StepBottleneckDto {
  stepName:           string;
  avgDurationMinutes: number;
  occurrenceCount:    number;
}

export interface CompletionTrendDto {
  weekStart:          string;   // ISO date string (YYYY-MM-DD)
  avgDurationMinutes: number;
  instanceCount:      number;
}

export interface CompletionTimeAnalyticsDto {
  byDefinition: WorkflowCompletionTimeDto[];
  bottlenecks:  StepBottleneckDto[];
  weeklyTrend:  CompletionTrendDto[];
}

export interface TeamMemberPerformanceDto {
  userId:                string;
  displayName:           string;
  email:                 string;
  role:                  string;
  completedSteps:        number;
  completedWorkflows:    number;
  avgStepDurationMinutes: number;
  overdueRate:           number;
}

export interface RoleDto {
  id:          string;
  name:        string;
  description: string;
}

export interface RevenueMonthlyDto {
  monthStart:      string;   // ISO date string (YYYY-MM-DD)
  invoicedAmount:  number;
  paidAmount:      number;
}

export interface RevenueByWorkflowTypeDto {
  workflowType:  string;
  totalAmount:   number;
  invoiceCount:  number;
}

export interface RevenueByClientDto {
  clientName:    string;
  totalAmount:   number;
  paidAmount:    number;
  invoiceCount:  number;
}

export interface RevenueSummaryDto {
  totalInvoiced: number;
  totalPaid:     number;
  outstanding:   number;
}

export interface RevenueAnalyticsDto {
  summary:        RevenueSummaryDto;
  monthlyTrend:   RevenueMonthlyDto[];
  byWorkflowType: RevenueByWorkflowTypeDto[];
  byClient:       RevenueByClientDto[];
}

export type TeamSortField =
  | 'displayName'
  | 'role'
  | 'completedSteps'
  | 'completedWorkflows'
  | 'avgStepDurationMinutes'
  | 'overdueRate';
