export interface AuditLogEntry {
  id:           string;
  actorId:      string;
  actorEmail:   string;
  action:       string;
  resourceType: string;
  resourceId:   string | null;
  oldValueJson: string | null;
  newValueJson: string | null;
  timestamp:    string;
}

export interface AuditLogPagedResult {
  items:       AuditLogEntry[];
  totalCount:  number;
  page:        number;
  pageSize:    number;
  totalPages:  number;
  hasNext:     boolean;
  hasPrevious: boolean;
}

export const ACTION_LABELS: Record<string, string> = {
  'user.invited':           'User Invited',
  'user.role_changed':      'Role Changed',
  'user.deactivated':       'User Deactivated',
  'user.reactivated':       'User Reactivated',
  'invoice.generated':      'Invoice Created',
  'invoice.sent':           'Invoice Sent',
  'invoice.paid':           'Invoice Paid',
  'invoice.voided':         'Invoice Voided',
  'tenant_settings.updated':'Settings Updated',
};

export const ACTION_BADGE_CLASS: Record<string, string> = {
  'user.invited':           'badge--blue',
  'user.role_changed':      'badge--blue',
  'user.deactivated':       'badge--red',
  'user.reactivated':       'badge--green',
  'invoice.generated':      'badge--purple',
  'invoice.sent':           'badge--purple',
  'invoice.paid':           'badge--green',
  'invoice.voided':         'badge--red',
  'tenant_settings.updated':'badge--orange',
};
