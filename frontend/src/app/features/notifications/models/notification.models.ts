export type NotificationType =
  | 'WorkflowStarted'
  | 'WorkflowCompleted'
  | 'WorkflowFailed'
  | 'StepAssigned'
  | 'StepCompleted'
  | 'SystemAlert';

export interface NotificationDto {
  id: string;
  tenantId: string;
  recipientId: string;
  type: NotificationType;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

/** Icon + colour config per notification type. */
export const NOTIFICATION_TYPE_CONFIG: Record<
  NotificationType,
  { icon: string; colorClass: string; label: string }
> = {
  WorkflowStarted:   { icon: 'play',    colorClass: 'notif-type--started',   label: 'Workflow Started' },
  WorkflowCompleted: { icon: 'check',   colorClass: 'notif-type--completed', label: 'Workflow Completed' },
  WorkflowFailed:    { icon: 'x',       colorClass: 'notif-type--failed',    label: 'Workflow Failed' },
  StepAssigned:      { icon: 'user',    colorClass: 'notif-type--assigned',  label: 'Step Assigned' },
  StepCompleted:     { icon: 'check',   colorClass: 'notif-type--completed', label: 'Step Completed' },
  SystemAlert:       { icon: 'alert',   colorClass: 'notif-type--alert',     label: 'System Alert' },
};
