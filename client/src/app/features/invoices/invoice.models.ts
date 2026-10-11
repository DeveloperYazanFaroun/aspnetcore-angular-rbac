export type InvoiceStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';

export const INVOICE_STATUSES: readonly InvoiceStatus[] = ['Draft', 'Submitted', 'Approved', 'Rejected'];

export interface Invoice {
  id: string;
  number: string;
  customerName: string;
  amount: number;
  status: InvoiceStatus;
  createdById: string;
  createdByName: string;
  createdAtUtc: string;
  reviewedById: string | null;
  reviewedByName: string | null;
  reviewedAtUtc: string | null;
}

export interface InvoiceQuery {
  status: InvoiceStatus | '';
  search: string;
}

export interface SaveInvoiceRequest {
  customerName: string;
  amount: number;
}
