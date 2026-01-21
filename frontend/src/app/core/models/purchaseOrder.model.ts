export interface PurchaseOrder {
  id: string;
  title: string;
  description?: string;
  costCenterId?: string;
  costCenterName?: string;
  projectId?: string;
  projectName?: string;
  totalAmount: number;
  currency: string;
  status: string;
  createdAt: string;
  approvedAt?: string | null;
}

export interface CreatePurchaseOrderRequest {
  id: string;
  title: string;
  description?: string;
  costCenterId?: string;
  projectId?: string;
  totalAmount: number;
  currency?: string;
}
