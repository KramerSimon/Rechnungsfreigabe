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

  getSupplierName(supplierId: number): string {
    const supplier = this.suppliers.find(s => s.id === supplierId);
    return supplier ? supplier.name : '-';
  }

  getStatusName(statusId: number): string {
    const status = this.statuses.find(s => s.id === statusId);
    return status ? status.code : '-';
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
}
