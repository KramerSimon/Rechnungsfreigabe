import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

interface InvoiceDetail {
  invoiceNumber: string;
  supplier: string;
  amount: string;
  project?: string;
  costCenter?: string;
  purchaseOrder?: string;
  status: string;
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
    FormsModule
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss'
})
export class InvoiceDetailComponent implements OnInit {
  invoiceNumber: string = '';

  invoice: InvoiceDetail = {
    invoiceNumber: 'TS-554',
    supplier: 'TechSolutions',
    amount: '€ 2.300,00',
    project: 'Website Relaunch',
    costCenter: '4020 - IT',
    purchaseOrder: 'PO-99231',
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
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.invoiceNumber = this.route.snapshot.params['id'] || 'TS-554';
    // Hier würden normalerweise die Rechnungsdaten geladen werden
  }

  canApprove(): boolean {
    return !!(this.invoice.project && this.invoice.costCenter);
  }

  onApprove() {
    if (!this.canApprove()) {
      alert('Bitte füllen Sie alle Pflichtfelder aus (Projekt, Kostenstelle)');
      return;
    }

    if (confirm('Möchten Sie diese Rechnung wirklich freigeben?')) {
      this.invoice.status = 'Freigegeben';
      // Hier würde die API-Anfrage erfolgen
      alert('Rechnung wurde freigegeben. Status: Weitere Freigabe wird geprüft...');
    }
  }

  onReject() {
    const reason = prompt('Bitte geben Sie den Grund für die Ablehnung an:');

    if (reason && reason.trim()) {
      this.invoice.status = 'Abgelehnt';
      // Hier würde die API-Anfrage erfolgen
      alert('Rückmeldung an Buchhaltung verschickt.');
    } else if (reason !== null) {
      alert('Grund für Ablehnung ist ein Pflichtfeld.');
    }
  }
}
