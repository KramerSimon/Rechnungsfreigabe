// Invoice History interfaces
export interface InvoiceHistoryDto {
  id: number;
  invoiceId: number;
  action: string;
  actionType: string;
  actionSource: string;
  oldStatus?: string;
  newStatus?: string;
  fieldChanges?: { [key: string]: any };
  comments?: string;
  policyReference?: string;
  systemReason?: string;
  changedByUser?: {
    id: number;
    username: string;
    firstName: string;
    lastName: string;
    fullName?: string;
  };
  changedAt: string;
  importChannel?: string;
  displayIcon: string;
  displayColor: string;
}

export interface FieldChangeDto {
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  displayName: string;
}

export interface InvoiceHistoryTimelineDto {
  date: string; // "HEUTE", "GESTERN", "10. DEZEMBER 2023"
  entries: InvoiceHistoryDto[];
}

export interface CreateHistoryEntryDto {
  invoiceId: number;
  action: string;
  actionType: string;
  actionSource: string;
  oldStatus?: string;
  newStatus?: string;
  fieldChanges?: FieldChangeDto[];
  comments?: string;
  policyReference?: string;
  systemReason?: string;
  changedBy?: number;
  importChannel?: string;
}

// Enums for better type safety
export enum HistoryActionType {
  Created = 'Created',
  Updated = 'Updated',
  StatusChanged = 'StatusChanged',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Escalated = 'Escalated',
  Assigned = 'Assigned',
  SystemAction = 'SystemAction',
  DataCompleted = 'DataCompleted',
  PaymentInitiated = 'PaymentInitiated',
  PolicyTriggered = 'PolicyTriggered'
}

export enum HistoryActionSource {
  User = 'User',
  System = 'System',
  Policy = 'Policy',
  Escalation = 'Escalation',
  Import = 'Import'
}
