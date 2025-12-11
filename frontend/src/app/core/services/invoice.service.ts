import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Supplier {
  id: number;
  name: string;
  legalName?: string;
  email?: string;
  phone?: string;
  isActive: boolean;
}

export interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  supplier: Supplier;
  purchaseOrderId?: string;
  costCenterId?: string;
  costCenterName?: string;
  projectId?: string;
  projectName?: string;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  invoiceDate: string;
  dueDate: string;
  receivedDate: string;
  status: string;
  requiresApproval: boolean;
  approvalLevel: number;
  autoApproved: boolean;
  description?: string;
  internalNotes?: string;
  creator?: User;
  processor?: User;
  createdAt: string;
  updatedAt: string;
  isOverdue: boolean;
  daysOverdue: number;
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

export interface PageRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  searchTerm?: string;
  status?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private readonly apiUrl = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  getInvoices(pageRequest?: PageRequest): Observable<PagedResult<Invoice>> {
    let params = new HttpParams();

    if (pageRequest) {
      if (pageRequest.pageNumber) params = params.set('pageNumber', pageRequest.pageNumber.toString());
      if (pageRequest.pageSize) params = params.set('pageSize', pageRequest.pageSize.toString());
      if (pageRequest.sortBy) params = params.set('sortBy', pageRequest.sortBy);
      if (pageRequest.sortOrder) params = params.set('sortOrder', pageRequest.sortOrder);
      if (pageRequest.searchTerm) params = params.set('searchTerm', pageRequest.searchTerm);
      if (pageRequest.status) params = params.set('status', pageRequest.status);
    }

    return this.http.get<PagedResult<Invoice>>(this.apiUrl, { params });
  }

  getInvoiceById(id: number): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.apiUrl}/${id}`);
  }

  createInvoice(invoice: Partial<Invoice>): Observable<Invoice> {
    return this.http.post<Invoice>(this.apiUrl, invoice);
  }

  updateInvoice(id: number, invoice: Partial<Invoice>): Observable<Invoice> {
    return this.http.put<Invoice>(`${this.apiUrl}/${id}`, invoice);
  }

  deleteInvoice(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  approveInvoice(id: number, approvalData: { approved: boolean; comments?: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, approvalData);
  }

  rejectInvoice(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, {
      approved: false,
      comments: reason
    });
  }

  getOverdueInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.apiUrl}/overdue`);
  }

  getPendingApprovals(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.apiUrl}/pending-approvals`);
  }
}
