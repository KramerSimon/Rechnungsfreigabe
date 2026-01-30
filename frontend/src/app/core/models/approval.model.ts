export interface ApprovalRule {
  id: number;
  name: string;
  description?: string;
  ruleType: 'Automatic' | 'Manual' | string;
  priority: number;
  isActive: boolean;
  conditions?: ApprovalRuleConditionDto[];
  actions?: ApprovalRuleActionDto[];
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
  createdBy: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface ApprovalRuleConditionDto {
  field: string;
  operator: string;
  value: string;
  logicalOperator?: 'AND' | 'OR';
}

export interface ApprovalRuleActionDto {
  actionType: string;
  actionValue?: string | null;
  description?: string | null;
  stages?: ApprovalRuleStageDto[];
}

export interface ApprovalRuleStageDto {
  stepNumber: number;
  approvalLevel: number;
  role?: string | null;
  userId?: number | null;
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
  ruleType: 'Automatic' | 'Manual' | string;
  priority?: number;
  conditions?: ApprovalRuleConditionDto[];
  actions?: ApprovalRuleActionDto[];
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
}

export interface UpdateApprovalRuleDto {
  name?: string;
  description?: string;
  ruleType?: 'Automatic' | 'Manual' | string;
  priority?: number;
  conditions?: ApprovalRuleConditionDto[];
  actions?: ApprovalRuleActionDto[];
  isActive?: boolean;
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
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
