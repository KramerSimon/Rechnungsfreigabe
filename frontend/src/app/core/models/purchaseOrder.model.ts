export interface PurchaseOrder {
  id: string;
  title: string;
  description?: string;
  totalAmount: number;
  currency: string;
  status: string;
  costCenterName?: string;
  projectName?: string;
  createdAt: string;
}
