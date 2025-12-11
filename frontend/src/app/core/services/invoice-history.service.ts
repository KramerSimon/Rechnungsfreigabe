import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InvoiceHistoryDto,
  InvoiceHistoryTimelineDto,
  CreateHistoryEntryDto
} from '../models/history.models';

@Injectable({
  providedIn: 'root'
})
export class InvoiceHistoryService {
  private readonly apiUrl = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  /**
   * Get invoice history as timeline
   */
  getInvoiceHistoryTimeline(invoiceId: number): Observable<InvoiceHistoryTimelineDto[]> {
    return this.http.get<InvoiceHistoryTimelineDto[]>(`${this.apiUrl}/${invoiceId}/history/timeline`);
  }

  /**
   * Get raw invoice history entries
   */
  getInvoiceHistory(invoiceId: number): Observable<InvoiceHistoryDto[]> {
    return this.http.get<InvoiceHistoryDto[]>(`${this.apiUrl}/${invoiceId}/history`);
  }

  /**
   * Create a manual history entry (admin only)
   */
  createHistoryEntry(invoiceId: number, entry: CreateHistoryEntryDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/${invoiceId}/history`, entry);
  }

  /**
   * Format time for display in timeline
   */
  formatTimeForDisplay(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleTimeString('de-DE', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  }

  /**
   * Get action description in German
   */
  getActionDescription(action: string, actionType: string, actionSource: string): string {
    const descriptions: { [key: string]: string } = {
      'Created': 'Rechnung erstellt',
      'Updated': 'Daten aktualisiert',
      'StatusChanged': 'Status geändert',
      'Approved': 'Freigabe erteilt',
      'Rejected': 'Freigabe abgelehnt',
      'Escalated': 'Eskalation',
      'Assigned': 'Zuweisung zur Freigabe',
      'SystemAction': 'Systemaktion',
      'DataCompleted': 'Daten vervollständigt',
      'PaymentInitiated': 'Zahlung angewiesen',
      'PolicyTriggered': 'Richtlinie ausgelöst'
    };

    return descriptions[actionType] || action;
  }

  /**
   * Get user display name
   */
  getUserDisplayName(entry: InvoiceHistoryDto): string {
    if (!entry.changedByUser) {
      return entry.actionSource === 'System' ? 'System' : 'Unbekannt';
    }

    return `${entry.changedByUser.firstName} ${entry.changedByUser.lastName}`;
  }

  /**
   * Get additional context for display
   */
  getContextualInfo(entry: InvoiceHistoryDto): string[] {
    const info: string[] = [];

    if (entry.actionSource === 'System' && entry.systemReason) {
      info.push(`System (${entry.systemReason})`);
    } else if (entry.actionSource === 'Policy' && entry.policyReference) {
      info.push(`Grund: Policy "${entry.policyReference}"`);
    } else if (entry.actionSource === 'Escalation') {
      info.push('System (Regel: Standard-Mahnwesen)');
    } else if (entry.importChannel) {
      info.push(`Kanal: ${entry.importChannel}`);
    }

    if (entry.oldStatus && entry.newStatus) {
      info.push(`Status geändert auf: ${entry.newStatus}`);
    }

    if (entry.fieldChanges) {
      const changes = Object.entries(entry.fieldChanges)
        .map(([field, change]: [string, any]) => {
          if (change.DisplayName && change.OldValue && change.NewValue) {
            return `+ ${change.DisplayName}: ${change.NewValue}`;
          }
          return null;
        })
        .filter(Boolean);

      info.push(...changes as string[]);
    }

    return info;
  }

  /**
   * Check if entry represents an escalation or urgent action
   */
  isEscalationOrUrgent(entry: InvoiceHistoryDto): boolean {
    return entry.actionType === 'Escalated' ||
           entry.actionSource === 'Escalation' ||
           entry.action.includes('ESKALATION') ||
           entry.action.includes('überschritten');
  }

  /**
   * Export history as PDF (placeholder for future implementation)
   */
  exportHistoryAsPdf(invoiceId: number): Observable<Blob> {
    // This would be implemented to call a backend endpoint that generates PDF
    return this.http.get(`${this.apiUrl}/${invoiceId}/history/export/pdf`, {
      responseType: 'blob'
    });
  }
}
