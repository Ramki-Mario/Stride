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
