// ClientStatus matches the backend enum: Active = 0, Inactive = 1
export type ClientStatus = 0 | 1;

export const CLIENT_STATUS_LABELS: Record<ClientStatus, string> = {
  0: 'Active',
  1: 'Inactive',
};

export interface ClientSummaryDto {
  id:            string;
  name:          string;
  contactPerson: string | null;
  email:         string | null;
  phone:         string | null;
  status:        ClientStatus;
  statusLabel:   string;
  createdAt:     string;   // ISO 8601 DateTime string
}

export interface ClientDetailDto extends ClientSummaryDto {
  address:   string | null;
  notes:     string | null;
  updatedAt: string;
}

export interface PagedResult<T> {
  items:           T[];
  totalCount:      number;
  page:            number;
  pageSize:        number;
  totalPages:      number;
  hasNextPage:     boolean;
  hasPreviousPage: boolean;
}

export interface CreateClientRequest {
  name:          string;
  contactPerson: string | null;
  email:         string | null;
  phone:         string | null;
  address:       string | null;
  notes:         string | null;
}

export interface UpdateClientRequest {
  name:          string;
  contactPerson: string | null;
  email:         string | null;
  phone:         string | null;
  address:       string | null;
  notes:         string | null;
}

// ── History DTOs (cross-module read) ──────────────────────────────────────────

export interface ClientWorkflowDto {
  id:           string;
  workflowName: string;
  status:       string;
  createdAt:    string;
  completedAt:  string | null;
}

export interface ClientInvoiceDto {
  id:            string;
  invoiceNumber: string;
  status:        number;
  statusLabel:   string;
  totalAmount:   number;
  currency:      string;
  createdAt:     string;
}

export interface ClientHistoryDto {
  clientId:   string;
  clientName: string;
  workflows:  ClientWorkflowDto[];
  invoices:   ClientInvoiceDto[];
}
