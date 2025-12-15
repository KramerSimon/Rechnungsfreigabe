// User and Role related models
export interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLogin?: string;
  createdAt: string;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  isSystemRole: boolean;
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

// DTOs for creating/updating users
export interface CreateUserData {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role?: string;
  roleIds: string[];
  isActive?: boolean;
  temporaryPassword?: string;
  activeDirectorySid?: string;
}

export interface UpdateUserData extends Partial<CreateUserData> {
  id: string;
}
