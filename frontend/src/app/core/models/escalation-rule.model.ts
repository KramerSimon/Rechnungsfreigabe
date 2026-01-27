export interface EscalationRule {
  id: number;
  name: string;
  description?: string;
  triggerStatus: string;
  triggerAfterHours: number;
  repeatIntervalHours?: number | null;
  maxEscalations?: number | null;
  notifyRole?: string | null;
  notifyRoleId?: number | null;
  notifyRoleName?: string | null;
  notifyUserId?: number | null;
  notifyUserName?: string | null;
  messageTemplate?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEscalationRuleDto {
  name: string;
  description?: string;
  triggerStatus: string;
  triggerAfterHours: number;
  repeatIntervalHours?: number | null;
  maxEscalations?: number | null;
  notifyRole?: string | null;
  notifyRoleId?: number | null;
  notifyUserId?: number | null;
  messageTemplate?: string | null;
  isActive?: boolean;
}
