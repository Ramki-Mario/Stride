export interface OverdueAlertItem {
  id: string;
  workflowName: string;
  clientName: string | null;
  minutesOverdue: number;
}

export interface UnassignedStepAlertItem {
  stepId: string;
  stepName: string;
  workflowInstanceId: string;
  workflowName: string;
  minutesWaiting: number;
}

export interface SlaAtRiskAlertItem {
  id: string;
  workflowName: string;
  clientName: string | null;
  minutesRemaining: number;
}

export interface ReadyToInvoiceAlertItem {
  id: string;
  workflowName: string;
  clientName: string | null;
  completedAt: string;
}

export interface DashboardAlertSummary {
  overdue: OverdueAlertItem[];
  unassignedSteps: UnassignedStepAlertItem[];
  slaAtRisk: SlaAtRiskAlertItem[];
  readyToInvoice: ReadyToInvoiceAlertItem[];
}
