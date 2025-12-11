import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { FormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { InvoiceHistoryTimelineComponent } from '../components/invoice-history-timeline/invoice-history-timeline.component';
import { InvoiceService, Invoice, Supplier } from '../core/services/invoice.service';
import { AuthService } from '../core/services/auth.service';

interface InvoiceDetail {
  id: number;
  invoiceNumber: string;
  supplier: Supplier;
  totalAmount: number;
  currency?: string;
  projectName?: string;
  costCenterName?: string;
  purchaseOrderId?: string;
  status: string;
  netAmount?: number;
  taxAmount?: number;
  invoiceDate?: string;
  dueDate?: string;
  description?: string;
}

@Component({
  selector: 'app-invoice-detail',
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatDialogModule,
    MatTabsModule,
    FormsModule,
    MatSnackBarModule,
    InvoiceHistoryTimelineComponent
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss'
})
export class InvoiceDetailComponent implements OnInit {
  invoiceId: number = 0;
  invoiceNumber: string = '';
  loading = false;

  invoice: InvoiceDetail = {
    id: 1, // This would come from route parameters in real implementation
    invoiceNumber: 'TS-554',
    supplier: { id: 1, name: 'TechSolutions', isActive: true },
    totalAmount: 2300,
    currency: 'EUR',
    projectName: 'Website Relaunch',
    costCenterName: '4020 - IT',
    purchaseOrderId: 'PO-99231',
    status: 'Wartet auf User-Freigabe'
  };

  costCenters = [
    { value: '4020', label: '4020 - IT' },
    { value: '4010', label: '4010 - Marketing' },
    { value: '4030', label: '4030 - Verwaltung' },
    { value: '4040', label: '4040 - Vertrieb' }
  ];

  purchaseOrders = [
    { value: 'PO-99231', label: 'PO-99231' },
    { value: 'PO-99232', label: 'PO-99232' },
    { value: 'PO-99233', label: 'PO-99233' }
  ];

  note: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private invoiceService: InvoiceService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.invoiceId = parseInt(this.route.snapshot.params['id']) || 1;
    this.loadInvoice();
  }

  private loadInvoice(): void {
    this.loading = true;
    this.invoiceService.getInvoiceById(this.invoiceId).subscribe({
      next: (invoice) => {
        this.invoice = invoice;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading invoice:', error);
        this.loading = false;
        // Fallback zu Demo-Daten wenn API fehlt
        this.invoice = {
          id: this.invoiceId,
          invoiceNumber: 'MS-2024-001',
          supplier: { id: 1, name: 'Microsoft Deutschland', isActive: true },
          totalAmount: 1000,
          currency: 'EUR',
          status: 'Freigabe_Erforderlich'
        };
      }
    });
  }

  canApprove(): boolean {
    return this.invoice !== null && !this.loading;
  }

  onApprove() {
    if (!this.invoice || !this.canApprove()) {
      this.snackBar.open('Rechnung kann nicht freigegeben werden', 'OK', { duration: 3000 });
      return;
    }

    if (confirm('Möchten Sie diese Rechnung wirklich freigeben?')) {
      this.loading = true;

      this.invoiceService.approveInvoice(this.invoice.id, {
        approved: true,
        comments: this.note || 'Freigabe erteilt'
      }).subscribe({
        next: (response) => {
          this.snackBar.open('Rechnung wurde erfolgreich freigegeben!', 'OK', { duration: 5000 });
          this.loading = false;
          // Zurück zum Dashboard
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('Error approving invoice:', error);
          this.snackBar.open('Fehler bei der Freigabe', 'OK', { duration: 5000 });
          this.loading = false;
        }
      });
    }
  }

  onReject() {
    if (!this.invoice) return;

    const reason = prompt('Bitte geben Sie den Grund für die Ablehnung an:');

    if (reason && reason.trim()) {
      this.loading = true;

      this.invoiceService.approveInvoice(this.invoice.id, {
        approved: false,
        comments: reason.trim()
      }).subscribe({
        next: (response) => {
          this.snackBar.open('Rechnung wurde abgelehnt', 'OK', { duration: 5000 });
          this.loading = false;
          // Zurück zum Dashboard
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('Error rejecting invoice:', error);
          this.snackBar.open('Fehler bei der Ablehnung', 'OK', { duration: 5000 });
          this.loading = false;
        }
      });
    } else if (reason !== null) {
      this.snackBar.open('Grund für Ablehnung ist ein Pflichtfeld', 'OK', { duration: 3000 });
    }
  }
}
