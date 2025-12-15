// Dashboard related models
export interface SystemStatus {
  servicesActive: boolean;
  autoApprovalRate: number;
  lastUpdate: string;
  totalInvoicesThisMonth: number;
  averageProcessingTime: number;
}

export interface UserTaskSummary {
  totalTasks: number;
  urgentCount: number;
  incompleteCount: number;
  overdueCount: number;
}

export interface AccountingOverview {
  rejectedCount: number;
  rejectedAmount: number;
  readyForPaymentCount: number;
  readyForPaymentAmount: number;
  openVolumeAmount: number;
  autoApprovalRate: number;
}

export interface AutoApprovalRule {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  condition: string;
}

export interface AssignmentRule {
  id: number;
  costCenter: string;
  assignedTo: string;
  isActive: boolean;
}

export interface MasterDataSection {
  id: string;
  title: string;
  description: string;
  icon: string;
  count?: number;
  route: string;
}

export interface UserTask {
  id: string;
  title: string;
  description: string;
  dueDate: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  status: 'pending' | 'in-progress' | 'completed';
  type: 'approval' | 'review' | 'processing';
}
