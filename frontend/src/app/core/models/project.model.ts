export interface Project {
  id: string;
  name: string;
  description?: string;
  costCenterId?: string;
  costCenterName?: string;
  costCenter?: string;
  budget?: number;
  spentAmount?: number;
  status?: string;
  startDate?: string;
  endDate?: string;
  projectManagerId?: number;
  projectManager?: {
    id: number;
    username: string;
    firstName?: string;
    lastName?: string;
    email?: string;
  };
}
