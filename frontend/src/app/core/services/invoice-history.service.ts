import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InvoiceHistoryDto,
  InvoiceHistoryTimelineDto,
  CreateHistoryEntryDto
} from '../models/history.models';
import { LanguageService } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class InvoiceHistoryService {
  private readonly apiUrl = `${environment.apiUrl}/v1/invoices`;

  constructor(
    private http: HttpClient,
    private languageService: LanguageService
  ) {}

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
    const descriptions: Record<string, string> = {
      Created: 'invoice.history.action.created',
      Updated: 'invoice.history.action.updated',
      StatusChanged: 'invoice.history.action.statusChanged',
      Approved: 'invoice.history.action.approved',
      Rejected: 'invoice.history.action.rejected',
      Escalated: 'invoice.history.action.escalated',
      Assigned: 'invoice.history.action.assigned',
      SystemAction: 'invoice.history.action.systemAction',
      DataCompleted: 'invoice.history.action.dataCompleted',
      PaymentInitiated: 'invoice.history.action.paymentInitiated',
      PolicyTriggered: 'invoice.history.action.policyTriggered'
    };

    return descriptions[actionType] ? this.t(descriptions[actionType]) : action;
  }

  /**
   * Get user display name
   */
  getUserDisplayName(entry: InvoiceHistoryDto): string {
    if (!entry.changedByUser) {
      return entry.actionSource === 'System'
        ? this.t('invoice.history.user.system')
        : this.t('invoice.history.user.unknown');
    }

    return `${entry.changedByUser.firstName} ${entry.changedByUser.lastName}`;
  }

  /**
   * Get additional context for display
   */
  getContextualInfo(entry: InvoiceHistoryDto): string[] {
    const info: string[] = [];

    if (entry.actionSource === 'System' && entry.systemReason) {
      info.push(this.tp('invoice.history.context.systemReason', { reason: entry.systemReason }));
    } else if (entry.actionSource === 'Policy' && entry.policyReference) {
      info.push(this.tp('invoice.history.context.policyReason', { policy: entry.policyReference }));
    } else if (entry.actionSource === 'Escalation') {
      info.push(this.t('invoice.history.context.defaultEscalationRule'));
    } else if (entry.importChannel) {
      info.push(this.tp('invoice.history.context.channel', { channel: entry.importChannel }));
    }

    if (entry.oldStatus && entry.newStatus) {
      info.push(this.tp('invoice.history.context.statusChangedTo', { status: entry.newStatus }));
    }

    if (entry.fieldChanges) {
      const changes = Object.entries(entry.fieldChanges)
        .map(([field, change]: [string, any]) => {
          if (change.DisplayName && change.OldValue && change.NewValue) {
            return this.tp('invoice.history.context.fieldChange', {
              field: change.DisplayName,
              value: change.NewValue
            });
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

  private t(key: string): string {
    return this.languageService.translateKey(key);
  }

  private tp(key: string, params: Record<string, string | number>): string {
    let translated = this.t(key);
    for (const [name, value] of Object.entries(params)) {
      translated = translated.replace(`{${name}}`, String(value));
    }
    return translated;
  }
}
