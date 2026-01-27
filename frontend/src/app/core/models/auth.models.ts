export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: User;
  permissions: string[];
}

export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  createdAt: string;
  roles: Role[];
}

export interface Role {
  id: number;
  name: string;
  description: string;
  permissions: Permission[];
  color?: string;
  isSystemRole?: boolean;
}

export interface Permission {
  id: number;
  name: string;
  description: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  permissions: string[];
}
