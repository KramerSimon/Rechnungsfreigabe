// Status model matching backend structure
export interface Status {
  id: number;
  code: string;
  displayName: string;
  description?: string;
  entityType: string;
  sortOrder: number;
  isActive: boolean;
  color?: string;
  createdAt: string;
}

// Status code constants matching backend
export const StatusCodes = {
  Invoice: {
    Eingegangen: 'Received',
    InPruefung: 'Under_Review',
    FreigabeErforderlich: 'Approval_Required',
    Freigegeben: 'Approved',
    Abgelehnt: 'Rejected',
    Bezahlt: 'Paid',
    Ueberfaellig: 'Overdue',
    Storniert: 'Cancelled'
  },
  Project: {
    Geplant: 'Planned',
    InArbeit: 'Active',
    Abgeschlossen: 'Completed',
    Pausiert: 'On_Hold',
    Abgebrochen: 'Cancelled'
  },
  PurchaseOrder: {
    Entwurf: 'Open',
    Genehmigt: 'Fulfilled',
    Gesendet: 'Partially_Fulfilled',
    Storniert: 'Cancelled'
  },
  ApprovalWorkflow: {
    Pending: 'Pending',
    Approved: 'Approved',
    Rejected: 'Rejected',
    Waiting: 'Waiting',
    Escalated: 'Escalated'
  }
};

// Entity types
export const EntityTypes = {
  Invoice: 'Invoice',
  Project: 'Project',
  PurchaseOrder: 'PurchaseOrder',
  ApprovalWorkflow: 'ApprovalWorkflow'
};
