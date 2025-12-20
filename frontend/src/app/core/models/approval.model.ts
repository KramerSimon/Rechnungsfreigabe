export interface ApprovalRule {
  id: number;
  name: string;
  description?: string;
  ruleType: string;
  priority: number;
  isActive: boolean;
  conditions?: string;
  actions?: string;
  createdBy: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface ApprovalWorkflow {
  id: number;
  invoiceId: number;
  ruleId?: number;
  stepNumber: number;
  approverId: number;
  approverName?: string;
  approvalLevel: number;
  status: string;
  comments?: string;
  approvedAt?: Date;
  createdAt: Date;
}

export interface CreateApprovalRuleDto {
  name: string;
  description?: string;
  ruleType: string;
  priority?: number;
  conditions?: string;
  actions?: string;
  isActive?: boolean;
}

export interface CreateApprovalWorkflowDto {
  invoiceId: number;
  ruleId?: number;
  stepNumber?: number;
  approverId: number;
  approvalLevel?: number;
  status?: string; // Pending | Waiting | Approved | Rejected | Skipped
  comments?: string;
}

export interface UpdateApprovalWorkflowDto {
  invoiceId?: number;
  ruleId?: number;
  stepNumber?: number;
  approverId?: number;
  approvalLevel?: number;
  status?: string; // Pending | Waiting | Approved | Rejected | Skipped
  comments?: string;
}
