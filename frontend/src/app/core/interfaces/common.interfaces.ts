// Common interfaces shared across the application
export interface PageRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  searchTerm?: string;
  status?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface BaseEntity {
  id: string | number;
  createdAt: string;
  updatedAt: string;
}

export interface SelectOption {
  id: string;
  label: string;
  value?: any;
  disabled?: boolean;
}

export interface TabConfig {
  id: string;
  label: string;
  index: number;
  icon?: string;
  disabled?: boolean;
}
