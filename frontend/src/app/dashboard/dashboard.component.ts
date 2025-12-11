import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { InvoiceService, Invoice, PageRequest } from '../core/services/invoice.service';
import { catchError, finalize, of } from 'rxjs';

interface DashboardInvoice {
  id: number;
  statusIcon: string;
  statusText: string;
  statusDetail?: string;
  statusClass: string;
  supplierName: string;
  invoiceNumber: string;
  amount: string;
  dueDate: string;
  actionText: string;
  isOverdue: boolean;
  originalInvoice: Invoice;
}

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  openInvoices = 0;
  overdueCount = 0;
  activeFilter = 'all';
  isLoading = false;
  error: string | null = null;

  displayedColumns: string[] = ['status', 'supplier', 'amount', 'dueDate', 'action'];

  constructor(
    private router: Router,
    private invoiceService: InvoiceService,
    private snackBar: MatSnackBar
  ) {}

  invoices: DashboardInvoice[] = [];
  allInvoices: DashboardInvoice[] = [];

  ngOnInit() {
    this.loadInvoices();
  }

  private loadInvoices() {
    this.isLoading = true;
    this.error = null;

    const pageRequest: PageRequest = {
      pageSize: 50, // Get more invoices for dashboard overview
      sortBy: 'dueDate',
      sortOrder: 'asc'
    };

    this.invoiceService.getInvoices(pageRequest)
      .pipe(
        catchError(error => {
          this.error = 'Fehler beim Laden der Rechnungen';
          this.snackBar.open('Fehler beim Laden der Rechnungen', 'Schließen', {
            duration: 5000,
            panelClass: ['error-snackbar']
          });
          console.error('Error loading invoices:', error);
          return of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 50, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
        }),
        finalize(() => this.isLoading = false)
      )
      .subscribe(result => {
        this.allInvoices = result.items.map(invoice => this.mapToDisplayInvoice(invoice));
        this.updateDashboardStats();
        this.applyFilter();
      });
  }

  private mapToDisplayInvoice(invoice: Invoice): DashboardInvoice {
    const statusInfo = this.getStatusInfo(invoice.status, invoice.isOverdue, invoice.requiresApproval);

    return {
      id: invoice.id,
      statusIcon: statusInfo.icon,
      statusText: statusInfo.text,
      statusDetail: statusInfo.detail,
      statusClass: statusInfo.class,
      supplierName: invoice.supplier.name,
      invoiceNumber: invoice.invoiceNumber,
      amount: this.formatCurrency(invoice.totalAmount, invoice.currency),
      dueDate: this.formatDate(invoice.dueDate),
      actionText: this.getActionText(invoice.status),
      isOverdue: invoice.isOverdue,
      originalInvoice: invoice
    };
  }

  private getStatusInfo(status: string, isOverdue: boolean, requiresApproval: boolean) {
    if (isOverdue) {
      return {
        icon: 'priority_high',
        text: 'ÜBERFÄLLIG',
        detail: '(Zahlungsziel überschritten)',
        class: 'status-urgent'
      };
    }

    switch (status) {
      case 'Eingegangen':
        return {
          icon: 'radio_button_unchecked',
          text: 'EINGEGANGEN',
          class: 'status-received'
        };
      case 'In_Pruefung':
        return {
          icon: 'schedule',
          text: 'IN PRÜFUNG',
          class: 'status-reviewing'
        };
      case 'Freigabe_Erforderlich':
        return {
          icon: 'approval',
          text: 'FREIGABE ERFORDERLICH',
          class: 'status-approval-required'
        };
      case 'Freigegeben':
        return {
          icon: 'check_circle',
          text: 'FREIGEGEBEN',
          class: 'status-approved'
        };
      case 'Abgelehnt':
        return {
          icon: 'cancel',
          text: 'ABGELEHNT',
          class: 'status-rejected'
        };
      case 'Bezahlt':
        return {
          icon: 'payment',
          text: 'BEZAHLT',
          class: 'status-paid'
        };
      default:
        return {
          icon: 'help_outline',
          text: 'UNBEKANNT',
          class: 'status-unknown'
        };
    }
  }

  private getActionText(status: string): string {
    switch (status) {
      case 'Freigabe_Erforderlich':
        return 'Freigeben';
      case 'In_Pruefung':
        return 'Prüfen';
      case 'Eingegangen':
        return 'Bearbeiten';
      default:
        return 'Anzeigen';
    }
  }

  private formatCurrency(amount: number, currency: string): string {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: currency || 'EUR'
    }).format(amount);
  }

  private formatDate(dateString: string): string {
    const date = new Date(dateString);
    const today = new Date();
    const diffTime = date.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
      return 'Heute';
    } else if (diffDays === -1) {
      return 'Gestern';
    } else if (diffDays === 1) {
      return 'Morgen';
    } else if (diffDays < 0) {
      return `Vor ${Math.abs(diffDays)} Tagen`;
    } else {
      return date.toLocaleDateString('de-DE', {
        day: '2-digit',
        month: '2-digit',
        year: '2-digit'
      });
    }
  }

  private updateDashboardStats() {
    this.openInvoices = this.allInvoices.filter(inv =>
      ['Eingegangen', 'In_Pruefung', 'Freigabe_Erforderlich'].includes(inv.originalInvoice.status)
    ).length;

    this.overdueCount = this.allInvoices.filter(inv => inv.isOverdue).length;
  }

  private applyFilter() {
    switch (this.activeFilter) {
      case 'open':
        this.invoices = this.allInvoices.filter(inv =>
          ['Eingegangen', 'In_Pruefung', 'Freigabe_Erforderlich'].includes(inv.originalInvoice.status)
        );
        break;
      case 'overdue':
        this.invoices = this.allInvoices.filter(inv => inv.isOverdue);
        break;
      case 'approval':
        this.invoices = this.allInvoices.filter(inv =>
          inv.originalInvoice.status === 'Freigabe_Erforderlich'
        );
        break;
      default:
        this.invoices = this.allInvoices;
    }
  }

  setFilter(filter: string) {
    this.activeFilter = filter;
    this.applyFilter();
  }

  handleAction(invoice: DashboardInvoice) {
    this.router.navigate(['/invoice-detail', invoice.id]);
  }

  refreshInvoices() {
    this.loadInvoices();
  }
}
