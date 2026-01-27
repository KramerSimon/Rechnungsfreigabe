import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { map, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserTaskSummary, AccountingOverview, SystemStatus } from '../models';

// Re-export for backward compatibility
export type { UserTaskSummary, AccountingOverview, SystemStatus };

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly apiUrl = `${environment.apiUrl}/v1/dashboards`;

  constructor(private http: HttpClient) {}

  /**
   * Lädt die Aufgaben-Übersicht für User-Dashboard
   */
  getUserTaskSummary(): Observable<UserTaskSummary> {
    return this.http.get<UserTaskSummary>(`${this.apiUrl}/user/summary`).pipe(
      catchError(error => {
        console.error('Error loading user task summary:', error);
        // Fallback mit Beispieldaten
        return of({
          totalTasks: 4,
          urgentCount: 1,
          incompleteCount: 1,
          overdueCount: 1
        });
      })
    );
  }

  /**
   * Lädt die Finanz-Übersicht für Accounting-Dashboard
   */
  getAccountingOverview(): Observable<AccountingOverview> {
    return this.http.get<AccountingOverview>(`${this.apiUrl}/accounting/overview`).pipe(
      catchError(error => {
        console.error('Error loading accounting overview:', error);
        // Fallback mit Beispieldaten
        return of({
          rejectedCount: 3,
          rejectedAmount: 7850,
          readyForPaymentCount: 12,
          readyForPaymentAmount: 45230,
          openVolumeAmount: 128450,
          autoApprovalRate: 35
        });
      })
    );
  }

  /**
   * Lädt den System-Status für Admin-Dashboard
   */
  getSystemStatus(): Observable<SystemStatus> {
    return this.http.get<SystemStatus>(`${this.apiUrl}/admin/status`).pipe(
      catchError(error => {
        console.error('Error loading system status:', error);
        // Fallback mit Beispieldaten
        return of({
          servicesActive: true,
          autoApprovalRate: 35,
          lastUpdate: new Date().toISOString(),
          totalInvoicesThisMonth: 284,
          averageProcessingTime: 2.3
        });
      })
    );
  }

  /**
   * Lädt Benutzer-spezifische Aufgaben (für User-Dashboard)
   */
  getUserTasks(filters?: { status?: string; limit?: number }): Observable<any[]> {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.limit) params.append('limit', filters.limit.toString());

    return this.http.get<any[]>(`${this.apiUrl}/user/tasks?${params.toString()}`).pipe(
      catchError(error => {
        console.error('Error loading user tasks:', error);
        return of([]);
      })
    );
  }

  /**
   * Lädt alle Rechnungen im Prozess (für Accounting-Dashboard)
   */
  getAccountingInvoices(filters?: {
    status?: string;
    search?: string;
    limit?: number
  }): Observable<any[]> {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.search) params.append('search', filters.search);
    if (filters?.limit) params.append('limit', filters.limit.toString());

    return this.http.get<any[]>(`${this.apiUrl}/accounting/invoices?${params.toString()}`).pipe(
      catchError(error => {
        console.error('Error loading accounting invoices:', error);
        return of([]);
      })
    );
  }

  /**
   * Löst Zahlungslauf aus (für Accounting-Dashboard)
   */
  initiatePaymentRun(invoiceIds: number[]): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/accounting/payment-run`,
      { invoiceIds }
    ).pipe(
      catchError(error => {
        console.error('Error initiating payment run:', error);
        return of({ success: false, message: 'Fehler beim Starten des Zahlungslaufs' });
      })
    );
  }

  /**
   * Lädt System-Konfigurationen (für Admin-Dashboard)
   */
  getSystemConfigurations(): Observable<{
    autoApprovalRules: any[];
    assignmentRules: any[];
    escalationSettings: any;
  }> {
    return this.http.get<any>(`${this.apiUrl}/admin/configurations`).pipe(
      catchError(error => {
        console.error('Error loading system configurations:', error);
        return of({
          autoApprovalRules: [],
          assignmentRules: [],
          escalationSettings: {}
        });
      })
    );
  }

  /**
   * Speichert System-Konfiguration (für Admin-Dashboard)
   */
  saveSystemConfiguration(
    type: 'autoApproval' | 'assignment' | 'escalation',
    configuration: any
  ): Observable<{ success: boolean; message: string }> {
    return this.http.put<{ success: boolean; message: string }>(
      `${this.apiUrl}/admin/configurations/${type}`,
      configuration
    ).pipe(
      catchError(error => {
        console.error('Error saving system configuration:', error);
        return of({ success: false, message: 'Fehler beim Speichern der Konfiguration' });
      })
    );
  }

  /**
   * Dashboard-Refresh für alle Dashboards
   */
  refreshDashboardData(dashboardType: 'user' | 'accounting' | 'admin'): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/refresh/${dashboardType}`, {}).pipe(
      catchError(error => {
        console.error('Error refreshing dashboard data:', error);
        return of({ success: false });
      })
    );
  }

  /**
   * Exportiert Dashboard-Daten als CSV/Excel
   */
  exportDashboardData(
    dashboardType: 'user' | 'accounting' | 'admin',
    format: 'csv' | 'excel' = 'csv'
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/export/${dashboardType}?format=${format}`,
      { responseType: 'blob' }
    ).pipe(
      catchError(error => {
        console.error('Error exporting dashboard data:', error);
        return of(new Blob());
      })
    );
  }

  /**
   * Lädt Dashboard-Metriken für Monitoring
   */
  getDashboardMetrics(dashboardType: 'user' | 'accounting' | 'admin'): Observable<{
    viewCount: number;
    lastAccessed: string;
    averageSessionTime: number;
    mostUsedFeatures: string[];
  }> {
    return this.http.get<any>(`${this.apiUrl}/metrics/${dashboardType}`).pipe(
      catchError(error => {
        console.error('Error loading dashboard metrics:', error);
        return of({
          viewCount: 0,
          lastAccessed: new Date().toISOString(),
          averageSessionTime: 0,
          mostUsedFeatures: []
        });
      })
    );
  }
}
