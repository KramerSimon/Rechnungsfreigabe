import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Invoice } from '../../../../core/models/invoice.models';
import { Supplier } from '../../../../core/models/supplier.model';
import { Status } from '../../../../core/models/status.model';
import { InvoiceService } from '../../../../core/services/invoice.service';
import { SupplierService } from '../../../../core/services/supplier.service';
import { StatusService } from '../../../../core/services/status.service';
import { StatusDisplayPipe } from '../../../../core/pipes/status-display.pipe';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-invoices-tab',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusDisplayPipe
  ],
  templateUrl: './invoices-tab.component.html',
  styleUrls: ['./invoices-tab.component.scss'],
})
export class InvoicesTabComponent implements OnInit {
  invoices: Invoice[] = [];
  suppliers: Supplier[] = [];
  statuses: Status[] = [];
  loadingInvoices = false;
  invoiceColumns = ['id', 'invoiceNumber', 'supplier', 'totalAmount', 'status', 'invoiceDate', 'actions'];

  constructor(
    private snackBar: MatSnackBar,
    private invoiceService: InvoiceService,
    private supplierService: SupplierService,
    private statusService: StatusService
  ) {}

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.loadingInvoices = true;
    forkJoin({
      invoices: this.invoiceService.getInvoices(),
      suppliers: this.supplierService.getSuppliers(),
      statuses: this.statusService.getAllStatuses()
    }).subscribe({
      next: (result: any) => {
        this.invoices = result.invoices.items || result.invoices;
        this.suppliers = result.suppliers;
        this.statuses = result.statuses;
        this.loadingInvoices = false;
      },
      error: (error: any) => {
        console.error('Error loading invoices:', error);
        this.loadingInvoices = false;
        this.snackBar.open('Fehler beim Laden der Rechnungen', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  getSupplierName(supplier?: Supplier | number | null): string {
    if (!supplier) return '-';

    if (typeof supplier === 'number') {
      const supplierEntity = this.suppliers.find(s => s.id === supplier);
      return supplierEntity ? supplierEntity.name : '-';
    }

    return supplier.name || '-';
  }

  getStatusName(statusCode: string): string {
    return statusCode || '-';
  }

  getContrastColor(hexColor: string): string {
    // Determine if text should be white or black based on background brightness
    if (!hexColor) return '#000000';

    const hex = hexColor.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);

    // Calculate luminance using relative luminance formula
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

    return luminance > 0.5 ? '#000000' : '#FFFFFF';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount);
  }

  formatDate(date: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('de-DE');
  }

  deleteInvoice(invoiceId: number, invoiceNumber: string): void {
    if (confirm(`Möchten Sie die Rechnung ${invoiceNumber} wirklich löschen? Dies löscht auch alle zugehörigen Genehmigungsworkflows.`)) {
      this.invoiceService.deleteInvoice(invoiceId).subscribe({
        next: () => {
          this.snackBar.open('Rechnung erfolgreich gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadInvoices();
        },
        error: (error: any) => {
          console.error('Fehler beim Löschen der Rechnung:', error);
          this.snackBar.open('Fehler beim Löschen der Rechnung', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }
}
