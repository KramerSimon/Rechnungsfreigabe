import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { InvoiceService, PagedResult } from '../../../../core/services/invoice.service';
import { Invoice } from '../../../../core/models';
import { catchError, finalize, of, forkJoin, skip } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LanguageService } from '../../../../core/services/language.service';
import { ManualWorkflowActionComponent } from './components/manual-workflow-action.component';

interface AccountingOverview {
  rejectedCount: number;
  rejectedAmount: number;
  readyForPaymentCount: number;
  readyForPaymentAmount: number;
  paidCount: number;
  paidAmount: number;
  openVolumeAmount: number;
}

interface AccountingInvoice {
  id: number;
  invoiceNumber: string;
  supplierName: string;
  status: string;
  statusDisplay: string;
  statusClass: string;
  statusColor?: string;
  assignedTo: string;
  amount: string;
  reason?: string;
  originalInvoice: Invoice;
}

@Component({
  selector: 'app-accounting-dashboard',
  templateUrl: './accounting-dashboard.component.html',
  styleUrls: ['./accounting-dashboard.component.scss'],
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatMenuModule,
    MatInputModule,
    MatSelectModule,
    MatFormFieldModule,
    FormsModule,
    ManualWorkflowActionComponent
  ]
})
export class AccountingDashboardComponent implements OnInit {
  loading = false;

  overview: AccountingOverview = {
    rejectedCount: 0,
    rejectedAmount: 0,
    readyForPaymentCount: 0,
    readyForPaymentAmount: 0,
    paidCount: 0,
    paidAmount: 0,
    openVolumeAmount: 0
  };

  searchTerm = '';
  selectedStatus = 'all';

  private sourceInvoices: Invoice[] = [];
  invoices: AccountingInvoice[] = [];
  filteredInvoices: AccountingInvoice[] = [];

  displayedColumns: string[] = ['id', 'supplier', 'status', 'assignedTo', 'amount', 'workflowAction'];

  statusOptions: Array<{ value: string; label: string }> = [];

  constructor(
    private invoiceService: InvoiceService,
    private router: Router,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.statusOptions = this.buildStatusOptions();
    this.languageService.currentLanguage$.pipe(skip(1)).subscribe(() => {
      this.statusOptions = this.buildStatusOptions();
      this.processAccountingData(this.sourceInvoices);
    });
    this.loadAccountingData();
  }

  private loadAccountingData(): void {
    this.loading = true;

    this.invoiceService.getAllInvoices({ pageNumber: 1, pageSize: 100 }).pipe(
      catchError(error => {
        console.error('Fehler beim Laden der Buchhaltungsdaten:', error);
        return of({ items: [], totalItems: 0, totalCount: 0, currentPage: 0, pageNumber: 0, pageSize: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
      }),
      finalize(() => this.loading = false)
    ).subscribe((response: PagedResult<Invoice>) => {
      this.processAccountingData(response.items);
    });
  }

  private processAccountingData(invoices: Invoice[]): void {
    this.sourceInvoices = [...invoices];
    this.invoices = invoices.map(invoice => this.mapInvoiceToAccountingView(invoice));
    this.calculateOverview();
    this.applyFilters();
  }

  private mapInvoiceToAccountingView(invoice: Invoice): AccountingInvoice {
    let statusDisplay: string;
    let statusClass: string;
    let assignedTo: string;
    let reason: string | undefined;

    const status = this.getEffectiveStatus(invoice);

    switch (status) {
      case 'rejected':
        statusDisplay = this.t('dashboard.accounting.status.rejectedKo');
        statusClass = 'status-rejected';
        assignedTo = this.getPendingApproverName(invoice) ?? '--';
        reason = this.t('dashboard.accounting.reason.wrongCostCenter');
        break;
      case 'approved':
        statusDisplay = this.t('dashboard.accounting.status.approved');
        statusClass = 'status-approved';
        assignedTo = this.t('dashboard.accounting.assigned.accounting');
        break;
      case 'paid':
        statusDisplay = this.t('dashboard.accounting.status.paid');
        statusClass = 'status-paid';
        assignedTo = this.t('dashboard.accounting.assigned.done');
        break;
      case 'approval_required':
        if (invoice.autoApproved) {
          statusDisplay = this.t('dashboard.accounting.status.autoApproved');
          statusClass = 'status-auto';
          assignedTo = this.t('dashboard.accounting.assigned.system');
          reason = this.t('dashboard.accounting.reason.smallAmountRule');
        } else {
          statusDisplay = this.t('dashboard.accounting.status.approvalRequired');
          statusClass = 'status-pending';
          assignedTo = this.getPendingApproverName(invoice) ?? this.t('dashboard.accounting.assigned.unassigned');
        }
        break;
      case 'received':
        statusDisplay = this.t('dashboard.accounting.status.received');
        statusClass = 'status-draft';
        assignedTo = this.getPendingApproverName(invoice) ?? this.t('dashboard.accounting.assigned.accounting');
        break;
      case 'overdue':
        statusDisplay = this.t('dashboard.accounting.status.overdue');
        statusClass = 'status-overdue';
        assignedTo = this.getPendingApproverName(invoice) ?? this.t('dashboard.accounting.assigned.unassigned');
        reason = this.t('dashboard.accounting.reason.deadlineExceeded');
        break;
      default:
        statusDisplay = this.t('dashboard.accounting.status.received');
        statusClass = 'status-draft';
        assignedTo = this.getPendingApproverName(invoice) ?? this.t('dashboard.accounting.assigned.accounting');
        break;
    }

    return {
      id: invoice.id,
      invoiceNumber: `#${invoice.invoiceNumber}`,
      supplierName: invoice.supplier.name,
      status,
      statusDisplay,
      statusClass,
      statusColor: invoice.statusColor,
      assignedTo,
      amount: `${invoice.totalAmount.toLocaleString(this.getLocale())} €`,
      reason,
      originalInvoice: invoice
    };
  }

  private getEffectiveStatus(invoice: Invoice): string {
    const rawStatus = (invoice.status || '').toLowerCase();
    const hasOpenApprovalStep = (invoice.pendingApprovals || []).some(aw => {
      const s = (aw?.status || '').toLowerCase();
      return s === 'pending' || s === 'waiting';
    });

    if (hasOpenApprovalStep && (rawStatus === 'received' || rawStatus === 'under_review')) {
      return 'approval_required';
    }

    return rawStatus;
  }

  getInvoiceCountLabel(count: number): string {
    return count === 1
      ? this.t('dashboard.accounting.invoice.single')
      : this.t('dashboard.accounting.invoice.plural');
  }

  private getPendingApproverName(invoice: Invoice): string | null {
    const workflows = (invoice.pendingApprovals || [])
      .filter(aw => {
        const status = aw?.status?.toLowerCase();
        return status === 'pending' || status === 'waiting';
      })
      .sort((a, b) => (a.stepNumber ?? 0) - (b.stepNumber ?? 0) ||
        new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

    const next = workflows.find(aw => aw?.status?.toLowerCase() === 'pending') || workflows[0];
    if (!next) {
      return null;
    }

    if (next.approver) {
      return `${next.approver.firstName} ${next.approver.lastName}`.trim();
    }

    return next.approverName || null;
  }

  private calculateOverview(): void {
    const rejectedInvoices = this.invoices.filter(inv => {
      const status = inv.status.toLowerCase();
      return status === 'rejected';
    });
    const approvedInvoices = this.invoices.filter(inv => {
      const status = inv.status.toLowerCase();
      return status === 'approved';
    });
    const paidInvoices = this.invoices.filter(inv => {
      const status = inv.status.toLowerCase();
      return status === 'paid';
    });
    const openVolumeInvoices = this.invoices.filter(inv => {
      const status = inv.status.toLowerCase();
      return status !== 'paid' && status !== 'rejected';
    });

    this.overview = {
      rejectedCount: rejectedInvoices.length,
      rejectedAmount: rejectedInvoices.reduce((sum, inv) => sum + inv.originalInvoice.totalAmount, 0),
      readyForPaymentCount: approvedInvoices.length,
      readyForPaymentAmount: approvedInvoices.reduce((sum, inv) => sum + inv.originalInvoice.totalAmount, 0),
      paidCount: paidInvoices.length,
      paidAmount: paidInvoices.reduce((sum, inv) => sum + inv.originalInvoice.totalAmount, 0),
      openVolumeAmount: openVolumeInvoices.reduce((sum, inv) => sum + inv.originalInvoice.totalAmount, 0)
    };
  }

  applyFilters(): void {
    let filtered = [...this.invoices];

    // Status Filter
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter(invoice => {
        const status = invoice.status.toLowerCase();
        switch (this.selectedStatus) {
          case 'rejected': return status === 'rejected';
          case 'approved': return status === 'approved';
          case 'paid': return status === 'paid';
          case 'auto_approved': return invoice.originalInvoice.autoApproved;
          case 'in_approval': return status === 'approval_required' && !invoice.originalInvoice.autoApproved;
          default: return true;
        }
      });
    }

    // Search Filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(invoice =>
        invoice.invoiceNumber.toLowerCase().includes(term) ||
        invoice.supplierName.toLowerCase().includes(term)
      );
    }

    this.filteredInvoices = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onStatusChange(): void {
    this.applyFilters();
  }

  onInvoiceClick(invoice: AccountingInvoice): void {
    this.router.navigate(['/invoice', invoice.id]);
  }

  canCreateManualWorkflow(invoice: AccountingInvoice): boolean {
    const status = (invoice.status || '').toLowerCase();
    const isOpenForManualCreation = status === 'received' || status === 'under_review';
    const hasOpenApprovals = (invoice.originalInvoice.pendingApprovals || []).some(aw => {
      const approvalStatus = (aw?.status || '').toLowerCase();
      return approvalStatus === 'pending' || approvalStatus === 'waiting';
    });

    return invoice.originalInvoice.requiresApproval && isOpenForManualCreation && !hasOpenApprovals;
  }

  onManualWorkflowCreated(): void {
    this.snackBar.open(this.t('dashboard.accounting.workflow.success'), 'OK', { duration: 3500 });
    this.loadAccountingData();
  }

  onManualWorkflowFailed(message: string): void {
    this.snackBar.open(message || this.t('dashboard.accounting.workflow.error'), 'OK', { duration: 4500 });
  }

  onResolveRejected(): void {
    // Navigation zu Verwaltung der abgelehnten Rechnungen
    console.log('Navigiere zu abgelehnten Rechnungen');
  }

  onInitiatePayment(): void {
    if (!this.authService.hasPermission('payments.process')) {
      this.snackBar.open(this.t('dashboard.accounting.snack.noPermission'), 'OK', { duration: 3000 });
      return;
    }

    const readyInvoices = this.invoices
      .filter(inv => inv.status.toLowerCase() === 'approved')
      .map(inv => inv.originalInvoice);

    if (!readyInvoices.length) {
      this.snackBar.open(this.t('dashboard.accounting.snack.noneReady'), 'OK', { duration: 3000 });
      return;
    }

    const confirmText = this.t('dashboard.accounting.confirm.runPayments')
      .replace('{count}', String(readyInvoices.length));
    if (!confirm(confirmText)) {
      return;
    }

    this.loading = true;
    forkJoin(readyInvoices.map(inv => this.invoiceService.updateInvoiceStatus(inv.id, 'Paid')))
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          this.snackBar.open(this.t('dashboard.accounting.snack.markedPaid'), 'OK', { duration: 4000 });
          this.loadAccountingData();
        },
        error: (error) => {
          console.error('Fehler beim Zahlungslauf:', error);
          this.snackBar.open(this.t('dashboard.accounting.snack.paymentRunFailed'), 'OK', { duration: 4000 });
        }
      });
  }

  onExportData(): void {
    // Export-Funktionalität
    console.log('Exportiere Daten');
  }

  formatAmount(amount: number): string {
    return `€ ${amount.toLocaleString(this.getLocale())},-`;
  }

  getCurrentMonth(): string {
    return new Date().toLocaleDateString(this.getLocale(), {
      year: 'numeric',
      month: 'long'
    });
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }

  private buildStatusOptions(): Array<{ value: string; label: string }> {
    return [
      { value: 'all', label: this.t('dashboard.accounting.filter.all') },
      { value: 'rejected', label: this.t('dashboard.accounting.filter.rejected') },
      { value: 'approved', label: this.t('dashboard.accounting.filter.approved') },
      { value: 'paid', label: this.t('dashboard.accounting.filter.paid') },
      { value: 'auto_approved', label: this.t('dashboard.accounting.filter.autoApproved') },
      { value: 'in_approval', label: this.t('dashboard.accounting.filter.inApproval') }
    ];
  }

  private getLocale(): string {
    switch (this.languageService.currentLanguage) {
      case 'en':
        return 'en-US';
      case 'it':
        return 'it-IT';
      default:
        return 'de-DE';
    }
  }

  getContrastColor(hexColor: string | undefined): string {
    if (!hexColor) {
      return 'white';
    }

    // Entferne das # und konvertiere zu RGB
    const hex = hexColor.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);

    // Berechne die Helligkeit (Luminance)
    const brightness = (r * 299 + g * 587 + b * 114) / 1000;

    // Wähle weiß oder schwarz basierend auf der Helligkeit
    return brightness > 155 ? '#000000' : '#ffffff';
  }
}
