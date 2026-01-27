// User and Role related models
export interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role?: string; // Legacy single role field
  roles?: RoleDto[]; // Array of roles from backend
  roleIds?: number[]; // Optional helper for update payloads
  isActive: boolean;
  lastLogin?: string;
  createdAt: string;
}

export interface RoleDto {
  id: number;
  name: string;
  description?: string;
  permissions: (string | number)[];
  isSystemRole?: boolean;
  color?: string;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  isSystemRole: boolean;
  color?: string;
}

export interface Permission {
  id: string;
  name: string;
  description: string;
  category: string;
}

export interface UserRole {
  userId: string;
  username: string;
  fullName: string;
  email: string;
  roles: string[];
}
