export interface CostCenter {
  id: string;
  name: string;
  description?: string;
  budget?: number;
  isActive?: boolean;
  managerId?: string;
  manager?: {
    id: number;
    username: string;
    firstName?: string;
    lastName?: string;
    email?: string;
  };
}
