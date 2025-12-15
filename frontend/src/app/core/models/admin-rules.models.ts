export interface ApprovalRule {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  ruleType: 'automatic' | 'manual';
  conditions: RuleCondition[];
  actions: RuleAction[];
  priority: number;
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
}

export interface RuleDialogData {
  rule?: ApprovalRule;
  mode: 'create' | 'edit';
}
