// Master data related models
export interface Supplier {
  id: number;
  name: string;
  legalName?: string;
  taxNumber?: string;
  email?: string;
  phone?: string;
  isActive: boolean;
}

export interface CostCenter {
  id: string;
  name: string;
  description?: string;
  budget?: number;
  isActive: boolean;
  managerName?: string;
}

export interface Project {
  id: string;
  name: string;
  description?: string;
  costCenterId: string;
  costCenterName?: string;
  budget?: number;
  status: string;
  startDate?: string;
  endDate?: string;
}

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

// DTOs for creating/updating master data
export interface CreateSupplierData {
  name: string;
  legalName?: string;
  taxNumber?: string;
  vatNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  postalCode?: string;
  city?: string;
  country?: string;
  email?: string;
  phone?: string;
  bankName?: string;
  iban?: string;
  bic?: string;
  paymentTermsDays?: number;
  isActive: boolean;
}

export interface CreateCostCenterData {
  id: string;
  name: string;
  description?: string;
  budget?: number;
  managerName?: string;
  managerId?: string;
}

export interface CreateProjectData {
  id: string;
  name: string;
  description?: string;
  costCenterId: string;
  budget?: number;
  startDate?: string;
  endDate?: string;
  status: string;
  projectManagerId?: string;
}
