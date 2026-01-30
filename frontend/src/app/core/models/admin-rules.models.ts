export interface ApprovalRule {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  ruleType: 'automatic' | 'manual';
  conditions: RuleCondition[];
  actions: RuleAction[];
  priority: number;
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
}

export interface RuleCondition {
  field: string;
  operator: string;
  value: string | number;
  logicalOperator?: 'AND' | 'OR';
}

export interface RuleAction {
  type: 'auto_approve' | 'require_approval' | 'set_status' | 'assign_to';
  value: string;
  description: string;
  stages?: StageDefinition[]; // Used when type === 'require_approval'
}

export interface StageDefinition {
  stepNumber: number;
  approvalLevel: number;
  role?: 'cost_center_manager' | 'project_manager' | 'manager' | 'admin';
  userId?: number;
}

export interface RuleDialogData {
  rule?: ApprovalRule;
  mode: 'create' | 'edit';
}
