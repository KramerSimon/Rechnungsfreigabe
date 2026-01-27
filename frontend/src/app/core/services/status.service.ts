import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Status, EntityTypes } from '../models/status.model';

@Injectable({
  providedIn: 'root'
})
export class StatusService {
  private readonly apiUrl = `${environment.apiUrl}/v1/statuses`;
  private allStatuses$ = new BehaviorSubject<Status[]>([]);
  private statusCache: Map<string, Status[]> = new Map();

  constructor(private http: HttpClient) {}

  /**
   * Get all statuses (with optional entity type filter)
   */
  getAllStatuses(): Observable<Status[]> {
    if (this.allStatuses$.value.length > 0) {
      return this.allStatuses$.asObservable();
    }

    return this.http.get<Status[]>(this.apiUrl).pipe(
      tap(statuses => {
        this.allStatuses$.next(statuses);
        this.buildCache(statuses);
      })
    );
  }

  /**
   * Get statuses for a specific entity type
   */
  getStatusesByEntityType(entityType: string): Observable<Status[]> {
    if (this.statusCache.has(entityType)) {
      return new Observable(observer => {
        observer.next(this.statusCache.get(entityType) || []);
        observer.complete();
      });
    }

    return this.getAllStatuses().pipe(
      map(statuses =>
        statuses.filter(
          s => s.entityType === entityType && s.isActive
        )
      )
    );
  }

  /**
   * Get a single status by code and entity type
   */
  getStatusByCode(code: string, entityType: string): Observable<Status | undefined> {
    return this.getAllStatuses().pipe(
      map(statuses =>
        statuses.find(s => s.code === code && s.entityType === entityType)
      )
    );
  }

  /**
   * Get invoice statuses
   */
  getInvoiceStatuses(): Observable<Status[]> {
    return this.getStatusesByEntityType(EntityTypes.Invoice);
  }

  /**
   * Get project statuses
   */
  getProjectStatuses(): Observable<Status[]> {
    return this.getStatusesByEntityType(EntityTypes.Project);
  }

  /**
   * Get purchase order statuses
   */
  getPurchaseOrderStatuses(): Observable<Status[]> {
    return this.getStatusesByEntityType(EntityTypes.PurchaseOrder);
  }

  /**
   * Get approval workflow statuses
   */
  getApprovalWorkflowStatuses(): Observable<Status[]> {
    return this.getStatusesByEntityType(EntityTypes.ApprovalWorkflow);
  }

  /**
   * Get status display name in current language
   */
  getStatusDisplayName(code: string, entityType: string): string {
    const status = this.statusCache
      .get(entityType)
      ?.find(s => s.code === code);
    return status?.displayName || code;
  }

  /**
   * Build cache for faster lookups
   */
  private buildCache(statuses: Status[]): void {
    const invoices = statuses.filter(s => s.entityType === EntityTypes.Invoice);
    const projects = statuses.filter(s => s.entityType === EntityTypes.Project);
    const purchaseOrders = statuses.filter(s => s.entityType === EntityTypes.PurchaseOrder);
    const approvalWorkflows = statuses.filter(s => s.entityType === EntityTypes.ApprovalWorkflow);

    this.statusCache.set(EntityTypes.Invoice, invoices);
    this.statusCache.set(EntityTypes.Project, projects);
    this.statusCache.set(EntityTypes.PurchaseOrder, purchaseOrders);
    this.statusCache.set(EntityTypes.ApprovalWorkflow, approvalWorkflows);
  }

  /**
   * Clear cache (useful for refresh scenarios)
   */
  clearCache(): void {
    this.allStatuses$.next([]);
    this.statusCache.clear();
  }
}
