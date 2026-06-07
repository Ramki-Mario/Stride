export type InvoiceStatus = 0 | 1 | 2 | 3; // Draft | Sent | Paid | Void

export const INVOICE_STATUS_LABELS: Record<InvoiceStatus, string> = {
  0: 'Draft',
  1: 'Sent',
  2: 'Paid',
  3: 'Void',
};

export const INVOICE_STATUS_CSS: Record<InvoiceStatus, string> = {
  0: 'inv-badge-draft',
  1: 'inv-badge-sent',
  2: 'inv-badge-paid',
  3: 'inv-badge-void',
};

/** Lightweight DTO returned by GET /bff/invoicing/invoices/by-workflow/{id}. */
export interface InvoiceReferenceDto {
  id:            string;
  invoiceNumber: string;
  status:        InvoiceStatus;
  statusLabel:   string;
}

export interface InvoiceSummaryDto {
  id:            string;
  invoiceNumber: string;
  clientName:    string;
  clientEmail:   string | null;
  currency:      string;
  status:        InvoiceStatus;
  statusLabel:   string;
  dueDate:       string;   // "YYYY-MM-DD"
  totalAmount:   number;
  createdAt:     string;
  sentAt:        string | null;
  paidAt:        string | null;
}

export interface InvoiceLineItemDto {
  id:          string;
  description: string;
  unitPrice:   number;
  quantity:    number;
  subtotal:    number;
}

export interface InvoiceDetailDto extends InvoiceSummaryDto {
  notes:                    string | null;
  lineItems:                InvoiceLineItemDto[];
  sourceWorkflowInstanceId: string | null;
}

export interface CreateWorkflowInvoiceBillableItemRequest {
  description: string;
  quantity:    number;
  unitPrice:   number;
  unit:        string;
}

export interface CreateWorkflowInvoiceRequest {
  workflowName: string;
  clientId:     string | null;
  billableItems: CreateWorkflowInvoiceBillableItemRequest[];
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

export interface GenerateInvoiceLineItemRequest {
  description: string;
  unitPrice:   number;
  quantity:    number;
}

export interface GenerateInvoiceRequest {
  invoiceNumber: string;
  clientName:    string;
  clientEmail:   string;
  currency:      string;
  dueDate:       string;  // "YYYY-MM-DD"
  notes:         string | null;
  lineItems:     GenerateInvoiceLineItemRequest[];
}
