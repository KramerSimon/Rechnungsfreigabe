import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { User, Invoice, PagedResult, PageRequest } from '../models';

// Re-export for backward compatibility
export type { PagedResult, PageRequest };

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private readonly apiUrl = `${environment.apiUrl}/v1/invoices`;

  constructor(private http: HttpClient) {}

  private mapInvoice(invoice: any): Invoice {
    // Behalte den vollständigen PDF-Pfad - der Backend wird damit umgehen
    const pdfPath = invoice.pdf_file_path || invoice.pdfFilePath;

    return {
      ...invoice,
      pdfFilePath: pdfPath,
      pdfFileName: invoice.original_filename || invoice.pdfFileName
    };
  }

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

    return this.http.get<PagedResult<Invoice>>(this.apiUrl, { params }).pipe(
      map((result: any) => {
        const rawItems = result?.items ?? result?.Items ?? [];

        return {
          ...result,
          items: Array.isArray(rawItems)
            ? rawItems.map((invoice: any) => this.mapInvoice(invoice))
            : []
        };
      })
    );
  }

  getAllInvoices(pageRequest?: PageRequest): Observable<PagedResult<Invoice>> {
    let params = new HttpParams();

    if (pageRequest) {
      if (pageRequest.pageNumber) params = params.set('pageNumber', pageRequest.pageNumber.toString());
      if (pageRequest.pageSize) params = params.set('pageSize', pageRequest.pageSize.toString());
      if (pageRequest.sortBy) params = params.set('sortBy', pageRequest.sortBy);
      if (pageRequest.sortOrder) params = params.set('sortOrder', pageRequest.sortOrder);
      if (pageRequest.searchTerm) params = params.set('searchTerm', pageRequest.searchTerm);
    }

    return this.http.get<PagedResult<Invoice>>(`${this.apiUrl}/all`, { params }).pipe(
      map((result: any) => {
        const rawItems = result?.items ?? result?.Items ?? [];

        return {
          ...result,
          items: Array.isArray(rawItems)
            ? rawItems.map((invoice: any) => this.mapInvoice(invoice))
            : []
        };
      })
    );
  }

  getInvoiceById(id: number): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.apiUrl}/${id}`).pipe(
      map(invoice => this.mapInvoice(invoice))
    );
  }

  createInvoice(invoice: Partial<Invoice>): Observable<Invoice> {
    return this.http.post<Invoice>(this.apiUrl, invoice);
  }

  updateInvoice(id: number, invoice: Partial<Invoice>): Observable<Invoice> {
    return this.http.put<Invoice>(`${this.apiUrl}/${id}`, invoice);
  }

  updateInvoiceStatus(id: number, status: string): Observable<Invoice> {
    return this.http.patch<Invoice>(`${this.apiUrl}/${id}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    });
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

  downloadInvoicePdf(invoiceId: number): Observable<Blob> {
    // Backend route is api/v1/pdf-upload/download/{invoiceId}
    return this.http.get(`${environment.apiUrl}/v1/pdf-upload/download/${invoiceId}`, {
      responseType: 'blob'
    });
  }
}
