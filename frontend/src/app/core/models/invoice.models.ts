// Invoice related models
import { Supplier } from './supplier.model';
import { User } from './user.models';

export interface Invoice {
  id: number;
  invoiceNumber: string;
  supplier: Supplier;
  purchaseOrderId?: string;
  costCenterId?: string;
  costCenterName?: string;
  projectId?: string;
  projectName?: string;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  invoiceDate: string;
  dueDate: string;
  receivedDate: string;
  status: string;
  requiresApproval: boolean;
  approvalLevel: number;
  autoApproved: boolean;
  description?: string;
  internalNotes?: string;
  creator?: User;
  processor?: User;
  createdAt: string;
  updatedAt: string;
  isOverdue: boolean;
  daysOverdue: number;
  pendingApprovals?: any[];
}

export interface InvoiceDetail extends Invoice {
  attachments?: InvoiceAttachment[];
  approvalHistory?: ApprovalHistoryEntry[];
  comments?: InvoiceComment[];
}

export interface InvoiceAttachment {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
  uploadedBy: User;
  url: string;
}

export interface ApprovalHistoryEntry {
  id: string;
  action: 'approved' | 'rejected' | 'requested' | 'forwarded';
  comment?: string;
  timestamp: string;
  user: User;
  level: number;
}

export interface InvoiceComment {
  id: string;
  text: string;
  timestamp: string;
  user: User;
  isInternal: boolean;
}

export interface CreateInvoiceData {
  supplierId: number;
  invoiceNumber: string;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  invoiceDate: string;
  dueDate: string;
  description?: string;
  costCenterId?: string;
  projectId?: string;
  purchaseOrderId?: string;
}

export interface UpdateInvoiceData extends Partial<CreateInvoiceData> {
  id: number;
  status?: string;
  internalNotes?: string;
}

export interface ApproveInvoiceData {
  comment?: string;
  forwardTo?: string;
}
