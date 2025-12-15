// Application-wide enums and constants

export enum InvoiceStatus {
  Eingegangen = 'Eingegangen',
  InPruefung = 'In_Pruefung',
  FreigabeErforderlich = 'Freigabe_Erforderlich',
  Freigegeben = 'Freigegeben',
  Abgelehnt = 'Abgelehnt',
  Bezahlt = 'Bezahlt',
  Ueberfaellig = 'Ueberfaellig',
  Storniert = 'Storniert'
}

export enum UserRoles {
  USER = 'USER',
  ACCOUNTING = 'ACCOUNTING',
  ADMIN = 'ADMIN',
  MANAGER = 'MANAGER'
}

export enum PermissionCategories {
  INVOICES = 'Rechnungen',
  USERS = 'Benutzerverwaltung',
  REPORTS = 'Berichte',
  SYSTEM = 'System'
}

export enum NotificationTypes {
  SUCCESS = 'success',
  WARNING = 'warning',
  ERROR = 'error',
  INFO = 'info'
}

// Application constants
export const APP_CONSTANTS = {
  DEFAULT_PAGE_SIZE: 10,
  MAX_FILE_SIZE_MB: 10,
  SUPPORTED_FILE_TYPES: ['.pdf', '.jpg', '.jpeg', '.png'],
  DATE_FORMAT: 'dd.MM.yyyy',
  CURRENCY_FORMAT: 'EUR',
  DEFAULT_LOCALE: 'de-DE'
} as const;

export const API_ENDPOINTS = {
  AUTH: '/api/auth',
  USERS: '/api/users',
  INVOICES: '/api/invoices',
  SUPPLIERS: '/api/suppliers',
  COST_CENTERS: '/api/cost-centers',
  DASHBOARD: '/api/dashboard'
} as const;

export const ROUTE_PATHS = {
  LOGIN: '/login',
  DASHBOARD: '/dashboard',
  ADMIN_DASHBOARD: '/dashboard/admin',
  USER_DASHBOARD: '/dashboard/user',
  ACCOUNTING_DASHBOARD: '/dashboard/accounting',
  ADMIN_USERS: '/admin/users',
  ADMIN_RULES: '/admin/rules',
  ADMIN_SUPPLIERS: '/admin/suppliers'
} as const;
