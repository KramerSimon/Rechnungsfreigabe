import { Injectable } from '@angular/core';

/**
 * Service to translate status codes and display names from English (backend) to German (frontend)
 */
@Injectable({
  providedIn: 'root'
})
export class StatusTranslatorService {
  private readonly statusTranslations: Record<string, Record<string, string>> = {
    Invoice: {
      'Received': 'Eingegangen',
      'Under_Review': 'Unter Prüfung',
      'Approval_Required': 'Genehmigung erforderlich',
      'Approved': 'Freigegeben',
      'Rejected': 'Abgelehnt',
      'Paid': 'Bezahlt',
      'Overdue': 'Überfällig',
      'Cancelled': 'Storniert'
    },
    Project: {
      'Planned': 'Geplant',
      'Active': 'Aktiv',
      'Completed': 'Abgeschlossen',
      'On_Hold': 'Pausiert',
      'Cancelled': 'Abgebrochen'
    },
    PurchaseOrder: {
      'Open': 'Offen',
      'Fulfilled': 'Erfüllt',
      'Partially_Fulfilled': 'Teilweise erfüllt',
      'Cancelled': 'Storniert'
    },
    ApprovalWorkflow: {
      'Pending': 'Ausstehend',
      'Approved': 'Genehmigt',
      'Rejected': 'Abgelehnt',
      'Waiting': 'Wartend',
      'Escalated': 'Eskaliert'
    }
  };

  translate(statusCode: string, entityType: string = 'Invoice'): string {
    const translations = this.statusTranslations[entityType] || {};
    return translations[statusCode] || statusCode;
  }

  translateDisplayName(displayName: string, entityType: string = 'Invoice'): string {
    // If display name is already a known English status, translate it
    const translations = this.statusTranslations[entityType] || {};

    // Find the code that matches this display name and return German translation
    for (const [code, german] of Object.entries(translations)) {
      if (code === displayName || code.replace(/_/g, ' ') === displayName) {
        return german;
      }
    }

    // Check if it's already a known English display name
    if (Object.values(translations).includes(displayName)) {
      return displayName; // Already translated
    }

    // If display name looks like English (contains spaces, starts with capital), try to translate
    if (displayName && /^[A-Z]/.test(displayName)) {
      // Convert spaces to underscores and check
      const formatted = displayName.replace(/\s+/g, '_');
      if (translations[formatted]) {
        return translations[formatted];
      }

      // Check direct match (case-insensitive)
      for (const [code, german] of Object.entries(translations)) {
        if (code.toLowerCase() === displayName.toLowerCase() ||
            code.replace(/_/g, ' ').toLowerCase() === displayName.toLowerCase()) {
          return german;
        }
      }
    }

    return displayName;
  }
}
