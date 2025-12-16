import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BulkUploadResult {
  successfulUploads: UploadedFile[];
  failedUploads: FailedFile[];
  totalAttempts: number;
  successCount: number;
  failureCount: number;
}

export interface UploadedFile {
  fileName: string;
  invoiceId: number;
  invoiceNumber: string;
  size: number;
}

export interface FailedFile {
  fileName: string;
  errorMessage: string;
}

export interface PdfUploadStatus {
  totalInvoices: number;
  invoicesWithPdf: number;
  totalPdfSize: number;
  diskUsageBytes: number;
  uploadDirectory: string;
  maxFileSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class PdfUploadService {
  private apiUrl = `${environment.apiUrl}/pdfupload`;

  constructor(private http: HttpClient) { }

  /**
   * Upload a single PDF file
   */
  uploadInvoicePdf(
    file: File,
    supplierId: number,
    purchaseOrderId?: string,
    costCenterId?: string
  ): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('supplierId', supplierId.toString());
    if (purchaseOrderId) {
      formData.append('purchaseOrderId', purchaseOrderId);
    }
    if (costCenterId) {
      formData.append('costCenterId', costCenterId);
    }

    return this.http.post(`${this.apiUrl}/upload`, formData);
  }

  /**
   * Upload multiple PDF files
   */
  bulkUploadInvoicePdfs(
    files: File[],
    supplierId: number,
    purchaseOrderId?: string,
    costCenterId?: string
  ): Observable<BulkUploadResult> {
    const formData = new FormData();

    files.forEach((file) => {
      formData.append('files', file);
    });

    formData.append('supplierId', supplierId.toString());
    if (purchaseOrderId) {
      formData.append('purchaseOrderId', purchaseOrderId);
    }
    if (costCenterId) {
      formData.append('costCenterId', costCenterId);
    }

    return this.http.post<BulkUploadResult>(`${this.apiUrl}/bulk-upload`, formData);
  }

  /**
   * Download an invoice PDF
   */
  downloadInvoicePdf(invoiceId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download/${invoiceId}`, {
      responseType: 'blob'
    });
  }

  /**
   * Delete an invoice PDF
   */
  deleteInvoicePdf(invoiceId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete/${invoiceId}`);
  }

  /**
   * Get upload status and statistics
   */
  getUploadStatus(): Observable<PdfUploadStatus> {
    return this.http.get<PdfUploadStatus>(`${this.apiUrl}/status`);
  }
}
