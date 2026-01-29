import { RoleDto } from './user.models';

export interface EscalationRule {
  id: number;
  name: string;
  description?: string;
  triggerStatusIds: number[];
  triggerStatuses: StatusDto[];
  triggerAfterMinutes: number;
  repeatIntervalHours?: number | null;
  maxEscalations?: number | null;
  notifyRoleIds: number[];
  notifyRoles: RoleDto[];
  notifyUserIds: number[];
  notifyUsers: UserDto[];
  messageTemplate?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface StatusDto {
  id: number;
  code: string;
  displayName: string;
  entityType: string;
  color?: string;
}

export interface UserDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

export interface CreateEscalationRuleDto {
  name: string;
  description?: string;
  triggerStatusIds: number[];
  triggerAfterMinutes: number;
  repeatIntervalHours?: number | null;
  maxEscalations?: number | null;
  notifyRoleIds: number[];
  notifyUserIds: number[];
  messageTemplate?: string | null;
  isActive?: boolean;
}
