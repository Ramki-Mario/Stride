export interface ScheduleDefinitionSummaryDto {
  id:                   string;
  name:                 string;
  workflowDefinitionId: string;
  cronExpression:       string;
  isActive:             boolean;
  nextRunAt:            string | null;
  createdAt:            string;
}

export interface ScheduleDefinitionDto extends ScheduleDefinitionSummaryDto {
  description: string | null;
  updatedAt:   string;
}

export interface CreateScheduleRequest {
  name:                 string;
  description:          string | null;
  workflowDefinitionId: string;
  cronExpression:       string;
  isActive:             boolean;
}

export interface UpdateScheduleRequest {
  name:                 string;
  description:          string | null;
  workflowDefinitionId: string;
  cronExpression:       string;
}
