export interface SupplierSummary {
  id: number;
  name: string;
}

export interface InvoiceSummary {
  id: number;
  invoiceNumber: string;
  totalAmount: number;
  currency: string;
  status: string;
  supplier: SupplierSummary;
}

export interface NotificationDto {
  id: number;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  priority: string;
  actionUrl?: string | null;
  createdAt: string;
  readAt?: string | null;
  invoice?: InvoiceSummary | null;
}

export interface UnreadCountResponse {
  count: number;
}
