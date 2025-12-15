import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PurchaseOrder {
  id: string;
  title: string;
  description?: string;
  costCenterId?: string;
  costCenterName?: string;
  projectId?: string;
  projectName?: string;
  totalAmount: number;
  currency: string;
  status: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderService {
  private readonly apiUrl = `${environment.apiUrl}/purchaseorders`;

  constructor(private http: HttpClient) {}

  getAllPurchaseOrders(): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(this.apiUrl);
  }

  getPurchaseOrderById(id: string): Observable<PurchaseOrder> {
    return this.http.get<PurchaseOrder>(`${this.apiUrl}/${id}`);
  }
}
