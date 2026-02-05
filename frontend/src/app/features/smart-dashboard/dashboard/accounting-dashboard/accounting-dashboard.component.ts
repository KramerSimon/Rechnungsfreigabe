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
import { catchError, finalize, of, forkJoin } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

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
    FormsModule
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

  invoices: AccountingInvoice[] = [];
  filteredInvoices: AccountingInvoice[] = [];

  displayedColumns: string[] = ['id', 'supplier', 'status', 'assignedTo', 'amount'];

  statusOptions = [
    { value: 'all', label: 'Alle' },
    { value: 'rejected', label: 'Abgelehnt' },
    { value: 'approved', label: 'Freigegeben' },
    { value: 'paid', label: 'Bezahlt' },
    { value: 'auto_approved', label: 'Automatisch freigegeben' },
    { value: 'in_approval', label: 'Genehmigung erforderlich' }
  ];

  constructor(
    private invoiceService: InvoiceService,
    private router: Router,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
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
    this.invoices = invoices.map(invoice => this.mapInvoiceToAccountingView(invoice));
    this.calculateOverview();
    this.applyFilters();
  }

  private mapInvoiceToAccountingView(invoice: Invoice): AccountingInvoice {
    let statusDisplay: string;
    let statusClass: string;
    let assignedTo: string;
    let reason: string | undefined;

    const status = invoice.status.toLowerCase();

    switch (status) {
      case 'rejected':
        statusDisplay = 'Abgelehnt (KO)';
        statusClass = 'status-rejected';
        assignedTo = this.getPendingApproverName(invoice) ?? '--';
        reason = 'Falsche KST';
        break;
      case 'approved':
        statusDisplay = 'Freigegeben';
        statusClass = 'status-approved';
        assignedTo = 'Buchhaltung';
        reason = 'Wartet auf Zahlung';
        break;
      case 'paid':
        statusDisplay = 'Bezahlt';
        statusClass = 'status-paid';
        assignedTo = 'Erledigt';
        break;
      case 'approval_required':
        if (invoice.autoApproved) {
          statusDisplay = 'Automatisch freigegeben';
          statusClass = 'status-auto';
          assignedTo = 'System';
          reason = 'Regel: Kleinestbetr.';
        } else {
          statusDisplay = 'Genehmigung erforderlich';
          statusClass = 'status-pending';
          assignedTo = this.getPendingApproverName(invoice) ?? 'Unzugewiesen';
        }
        break;
      case 'received':
        statusDisplay = 'Eingegangen';
        statusClass = 'status-draft';
        assignedTo = this.getPendingApproverName(invoice) ?? 'Buchhaltung';
        break;
      case 'overdue':
        statusDisplay = 'Überfällig';
        statusClass = 'status-overdue';
        assignedTo = this.getPendingApproverName(invoice) ?? 'Unzugewiesen';
        reason = 'Frist überschritten';
        break;
      default:
        statusDisplay = 'Eingegangen';
        statusClass = 'status-draft';
        assignedTo = this.getPendingApproverName(invoice) ?? 'Buchhaltung';
        break;
    }

    return {
      id: invoice.id,
      invoiceNumber: `#${invoice.invoiceNumber}`,
      supplierName: invoice.supplier.name,
      status: invoice.status,
      statusDisplay,
      statusClass,
      statusColor: invoice.statusColor,
      assignedTo,
      amount: `${invoice.totalAmount.toLocaleString('de-DE')} €`,
      reason,
      originalInvoice: invoice
    };
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

  onResolveRejected(): void {
    // Navigation zu Verwaltung der abgelehnten Rechnungen
    console.log('Navigiere zu abgelehnten Rechnungen');
  }

  onInitiatePayment(): void {
    if (!this.authService.hasPermission('payments.process')) {
      this.snackBar.open('Keine Berechtigung zum Ausführen des Zahlungslaufs.', 'OK', { duration: 3000 });
      return;
    }

    const readyInvoices = this.invoices
      .filter(inv => inv.status.toLowerCase() === 'approved')
      .map(inv => inv.originalInvoice);

    if (!readyInvoices.length) {
      this.snackBar.open('Keine zahlungsbereiten Rechnungen gefunden.', 'OK', { duration: 3000 });
      return;
    }

    if (!confirm(`Zahlungslauf starten und ${readyInvoices.length} Rechnung(en) als bezahlt markieren?`)) {
      return;
    }

    this.loading = true;
    forkJoin(readyInvoices.map(inv => this.invoiceService.updateInvoiceStatus(inv.id, 'Paid')))
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          this.snackBar.open('Rechnungen wurden als bezahlt markiert.', 'OK', { duration: 4000 });
          this.loadAccountingData();
        },
        error: (error) => {
          console.error('Fehler beim Zahlungslauf:', error);
          this.snackBar.open('Zahlungslauf fehlgeschlagen.', 'OK', { duration: 4000 });
        }
      });
  }

  onExportData(): void {
    // Export-Funktionalität
    console.log('Exportiere Daten');
  }

  formatAmount(amount: number): string {
    return `€ ${amount.toLocaleString('de-DE')},-`;
  }

  getCurrentMonth(): string {
    return new Date().toLocaleDateString('de-DE', {
      year: 'numeric',
      month: 'long'
    });
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
