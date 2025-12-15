export interface PagedResult<T> {
  items: T[];
  totalItems: number;
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PageRequest {
  page?: number;
  pageSize?: number;
  pageNumber?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  sortOrder?: 'asc' | 'desc';
  searchQuery?: string;
  searchTerm?: string;
  status?: string;
}

export interface DashboardRoute {
  path: string;
  component: string;
  role: any; // UserRole from role.service.ts
  title: string;
}
