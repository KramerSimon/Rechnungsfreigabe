export interface CostCenter {
  id: string;
  name: string;
  description?: string;
  budget?: number;
  isActive?: boolean;
  managerName?: string;
  manager?: string;
}
