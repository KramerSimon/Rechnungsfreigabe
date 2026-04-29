export type LanguageCode = 'de' | 'en' | 'it';

export interface LanguageOption {
  code: LanguageCode;
  label: string;
}

export const LANGUAGE_OPTIONS: LanguageOption[] = [
  { code: 'de', label: 'Deutsch' },
  { code: 'en', label: 'English' },
  { code: 'it', label: 'Italiano' }
];

export const KEY_TRANSLATIONS: Record<LanguageCode, Record<string, string>> = {
  de: {
    'toolbar.notifications': 'Benachrichtigungen',
    'toolbar.theme': 'Dunklen Modus aktivieren',
    'toolbar.theme.light': 'Hellen Modus aktivieren',
    'toolbar.language': 'Sprache',
    'toolbar.editUser': 'Benutzer bearbeiten',
    'toolbar.logout': 'Abmelden'
  },
  en: {
    'toolbar.notifications': 'Notifications',
    'toolbar.theme': 'Enable dark mode',
    'toolbar.theme.light': 'Enable light mode',
    'toolbar.language': 'Language',
    'toolbar.editUser': 'Edit user',
    'toolbar.logout': 'Log out'
  },
  it: {
    'toolbar.notifications': 'Notifiche',
    'toolbar.theme': 'Attiva modalita scura',
    'toolbar.theme.light': 'Attiva modalita chiara',
    'toolbar.language': 'Lingua',
    'toolbar.editUser': 'Modifica utente',
    'toolbar.logout': 'Disconnetti'
  }
};

export const PHRASE_TRANSLATIONS: Record<LanguageCode, Record<string, string>> = {
  de: {
    'Zuruck zur Ubersicht': 'Zuruck zur Ubersicht',
    'Rechnungsdetails': 'Rechnungsdetails',
    'Historie & Aktivitaten': 'Historie & Aktivitaten',
    'Neue Regel erstellen': 'Neue Regel erstellen',
    'AKTIVE REGELN': 'AKTIVE REGELN',
    'Freigabe-Regeln': 'Freigabe-Regeln',
    'AD Integration': 'AD Integration',
    'DEINE OFFENEN AUFGABEN': 'DEINE OFFENEN AUFGABEN',
    'Alle': 'Alle',
    'Dringend': 'Dringend',
    'Warten': 'Warten',
    'STATUS': 'STATUS',
    'LIEFERANT': 'LIEFERANT',
    'BETRAG': 'BETRAG',
    'FALLIG': 'FALLIG',
    'AKTION': 'AKTION',
    'Freigeben': 'Freigeben',
    'ABLEHNEN': 'ABLEHNEN',
    'FREIGEBEN': 'FREIGEBEN',
    'Status:': 'Status:'
  },
  en: {
    'Zuruck zur Ubersicht': 'Back to overview',
    'Rechnungsdetails': 'Invoice details',
    'Historie & Aktivitaten': 'History & activities',
    'Neue Regel erstellen': 'Create new rule',
    'AKTIVE REGELN': 'ACTIVE RULES',
    'Freigabe-Regeln': 'Approval rules',
    'AD Integration': 'AD integration',
    'DEINE OFFENEN AUFGABEN': 'YOUR OPEN TASKS',
    'Alle': 'All',
    'Dringend': 'Urgent',
    'Warten': 'Waiting',
    'STATUS': 'STATUS',
    'LIEFERANT': 'SUPPLIER',
    'BETRAG': 'AMOUNT',
    'FALLIG': 'DUE',
    'AKTION': 'ACTION',
    'Freigeben': 'Approve',
    'ABLEHNEN': 'REJECT',
    'FREIGEBEN': 'APPROVE',
    'Status:': 'Status:'
  },
  it: {
    'Zuruck zur Ubersicht': 'Torna alla panoramica',
    'Rechnungsdetails': 'Dettagli fattura',
    'Historie & Aktivitaten': 'Cronologia e attivita',
    'Neue Regel erstellen': 'Crea nuova regola',
    'AKTIVE REGELN': 'REGOLE ATTIVE',
    'Freigabe-Regeln': 'Regole di approvazione',
    'AD Integration': 'Integrazione AD',
    'DEINE OFFENEN AUFGABEN': 'LE TUE ATTIVITA APERTE',
    'Alle': 'Tutte',
    'Dringend': 'Urgente',
    'Warten': 'In attesa',
    'STATUS': 'STATO',
    'LIEFERANT': 'FORNITORE',
    'BETRAG': 'IMPORTO',
    'FALLIG': 'SCADENZA',
    'AKTION': 'AZIONE',
    'Freigeben': 'Approva',
    'ABLEHNEN': 'RIFIUTA',
    'FREIGEBEN': 'APPROVA',
    'Status:': 'Stato:'
  }
};
