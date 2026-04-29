import { Injectable } from '@angular/core';
import { LanguageService } from './language.service';

/**
 * Service to translate status codes and display names from English (backend) to German (frontend)
 */
@Injectable({
  providedIn: 'root'
})
export class StatusTranslatorService {
  private readonly statusTranslations: Record<string, Record<string, Record<string, string>>> = {
    de: {
      Invoice: {
        'Received': 'Eingegangen',
        'Under_Review': 'Unter Pruefung',
        'Approval_Required': 'Genehmigung erforderlich',
        'Approved': 'Freigegeben',
        'Rejected': 'Abgelehnt',
        'Paid': 'Bezahlt',
        'Overdue': 'Ueberfaellig',
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
        'Fulfilled': 'Erfuellt',
        'Partially_Fulfilled': 'Teilweise erfuellt',
        'Cancelled': 'Storniert'
      },
      ApprovalWorkflow: {
        'Pending': 'Ausstehend',
        'Approved': 'Genehmigt',
        'Rejected': 'Abgelehnt',
        'Waiting': 'Wartend',
        'Escalated': 'Eskaliert'
      }
    },
    en: {
      Invoice: {
        'Received': 'Received',
        'Under_Review': 'Under review',
        'Approval_Required': 'Approval required',
        'Approved': 'Approved',
        'Rejected': 'Rejected',
        'Paid': 'Paid',
        'Overdue': 'Overdue',
        'Cancelled': 'Cancelled'
      },
      Project: {
        'Planned': 'Planned',
        'Active': 'Active',
        'Completed': 'Completed',
        'On_Hold': 'On hold',
        'Cancelled': 'Cancelled'
      },
      PurchaseOrder: {
        'Open': 'Open',
        'Fulfilled': 'Fulfilled',
        'Partially_Fulfilled': 'Partially fulfilled',
        'Cancelled': 'Cancelled'
      },
      ApprovalWorkflow: {
        'Pending': 'Pending',
        'Approved': 'Approved',
        'Rejected': 'Rejected',
        'Waiting': 'Waiting',
        'Escalated': 'Escalated'
      }
    },
    it: {
      Invoice: {
        'Received': 'Ricevuta',
        'Under_Review': 'In revisione',
        'Approval_Required': 'Approvazione richiesta',
        'Approved': 'Approvata',
        'Rejected': 'Rifiutata',
        'Paid': 'Pagata',
        'Overdue': 'Scaduta',
        'Cancelled': 'Annullata'
      },
      Project: {
        'Planned': 'Pianificato',
        'Active': 'Attivo',
        'Completed': 'Completato',
        'On_Hold': 'In sospeso',
        'Cancelled': 'Annullato'
      },
      PurchaseOrder: {
        'Open': 'Aperto',
        'Fulfilled': 'Completato',
        'Partially_Fulfilled': 'Parzialmente completato',
        'Cancelled': 'Annullato'
      },
      ApprovalWorkflow: {
        'Pending': 'In attesa',
        'Approved': 'Approvato',
        'Rejected': 'Rifiutato',
        'Waiting': 'In attesa',
        'Escalated': 'Escalato'
      }
    }
  };

  constructor(private languageService: LanguageService) {}

  translate(statusCode: string, entityType: string = 'Invoice'): string {
    const lang = this.languageService.currentLanguage;
    const translations = this.statusTranslations[lang]?.[entityType] || {};
    return translations[statusCode] || statusCode;
  }

  translateDisplayName(displayName: string, entityType: string = 'Invoice'): string {
    // If display name is already a known English status, translate it
    const lang = this.languageService.currentLanguage;
    const translations = this.statusTranslations[lang]?.[entityType] || {};

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
