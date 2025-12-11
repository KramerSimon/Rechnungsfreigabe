import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

interface Invoice {
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
}

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  openInvoices = 4;
  overdueCount = 1;
  activeFilter = 'all';

  displayedColumns: string[] = ['status', 'supplier', 'amount', 'dueDate', 'action'];

  constructor(private router: Router) {}

  invoices: Invoice[] = [
    {
      statusIcon: 'priority_high',
      statusText: 'EILT',
      statusDetail: '(Eskaliert)',
      statusClass: 'status-urgent',
      supplierName: 'Bürobedarf GmbH',
      invoiceNumber: '2023-99',
      amount: '€ 450,00',
      dueDate: 'Gestern',
      actionText: 'Bearbeiten',
      isOverdue: true
    },
    {
      statusIcon: 'radio_button_unchecked',
      statusText: 'Offen',
      statusClass: 'status-open',
      supplierName: 'TechSolutions',
      invoiceNumber: 'TS-554',
      amount: '€ 2.300,-',
      dueDate: '15.12.23',
      actionText: 'Bearbeiten',
      isOverdue: false
    },
    {
      statusIcon: 'help_outline',
      statusText: 'Daten fehlen',
      statusClass: 'status-incomplete',
      supplierName: 'Catering Müller',
      invoiceNumber: 'CM-12',
      amount: '€ 120,50',
      dueDate: '20.12.23',
      actionText: 'Ergänzen',
      isOverdue: false
    }
  ];

  setFilter(filter: string) {
    this.activeFilter = filter;
    // Hier könnte die Filterlogik implementiert werden
  }

  handleAction(invoice: Invoice) {
    this.router.navigate(['/invoice', invoice.invoiceNumber]);
  }
}
