export interface TeamMemberStep {
  stepId:             string;
  stepName:           string;
  workflowInstanceId: string;
  workflowName:       string;
  dueAt:              string | null;
  isOverdue:          boolean;
}

export interface TeamWorkloadItem {
  userId:          string;
  displayName:     string;
  email:           string;
  activeStepCount: number;
  hasOverdueSteps: boolean;
  topSteps:        TeamMemberStep[];
}
