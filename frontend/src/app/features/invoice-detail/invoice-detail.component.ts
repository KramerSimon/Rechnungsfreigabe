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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { InvoiceHistoryTimelineComponent } from '../invoice-history/invoice-history-timeline.component';
import { ApprovalTimelineComponent } from './approval-timeline.component';
import { StatusDisplayPipe } from '../../core/pipes/status-display.pipe';
import { InvoiceService, PagedResult } from '../../core/services/invoice.service';
import { Invoice, InvoiceDetail } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';
import { CostCenterService } from '../../core/services/cost-center.service';
import { PurchaseOrderService } from '../../core/services/purchase-order.service';
import { SupplierService } from '../../core/services/supplier.service';
import { CostCenter } from '../../core/models/cost-center.model';
import { Project } from '../../core/models/project.model';
import { PurchaseOrder } from '../../core/models/purchaseOrder.model';
import { Supplier } from '../../core/models/supplier.model';

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
    MatCheckboxModule,
    MatProgressSpinnerModule,
    NgxExtendedPdfViewerModule,
    InvoiceHistoryTimelineComponent,
    ApprovalTimelineComponent,
    StatusDisplayPipe
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss'
})
export class InvoiceDetailComponent implements OnInit {
  invoiceId: number = 0;
  invoiceNumber: string = '';
  loading = false;
  isEditMode = false;
  editFormDirty = false;
  pdfLoading = false;
  pdfUrl: SafeResourceUrl | null = null;
  pdfBlobUrl: string | null = null;
  pdfSrc: Uint8Array | null = null;

  invoice: InvoiceDetail = {
    id: 1, // This would come from route parameters in real implementation
    invoiceNumber: 'TS-554',
    supplier: { id: 1, name: 'TechSolutions', legal_name: 'TechSolutions GmbH' },
    totalAmount: 2300,
    netAmount: 1932.77,
    taxAmount: 367.23,
    currency: 'EUR',
    invoiceDate: '2024-01-15',
    dueDate: '2024-02-15',
    receivedDate: '2024-01-16',
    projectId: 'IT-NEU-001',
    projectName: 'Website Relaunch',
    costCenterId: 'IT',
    costCenterName: 'IT',
    purchaseOrderId: 'PO-99231',
    status: 'Wartet auf User-Freigabe',
    requiresApproval: true,
    approvalLevel: 1,
    autoApproved: false,
    description: 'Website Relaunch Projekt',
    createdAt: '2024-01-16T08:00:00Z',
    updatedAt: '2024-01-16T08:00:00Z',
    isOverdue: false,
    daysOverdue: 0,
    attachments: [],
    approvalHistory: [],
    comments: []
  };

  costCenters: CostCenter[] = [];
  projects: Project[] = [];
  purchaseOrders: PurchaseOrder[] = [];
  suppliers: Supplier[] = [];

  note: string = '';
  saving = false;
  saveStatus: 'success' | 'error' | null = null;
  saveMessage = '';
  lastSavedAt: Date | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private invoiceService: InvoiceService,
    private costCenterService: CostCenterService,
    private purchaseOrderService: PurchaseOrderService,
    private supplierService: SupplierService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit() {
    this.invoiceId = parseInt(this.route.snapshot.params['id']) || 1;
    this.loadCostCenters();
    this.loadSuppliers();
    this.loadInvoice();
  }

  private loadInvoice(): void {
    this.loading = true;
    this.invoiceService.getInvoiceById(this.invoiceId).subscribe({
      next: (invoice) => {
        this.invoice = invoice;
        this.loading = false;
        this.loadProjects(this.invoice.costCenterId);
        this.loadPurchaseOrders();
        this.loadPdf();
      },
      error: (error) => {
        console.error('Error loading invoice:', error);
        this.loading = false;
        this.pdfLoading = false;
        const message = error?.status === 404
          ? 'Rechnung wurde nicht gefunden. Bitte zurück zur Übersicht und erneut wählen.'
          : 'Rechnung konnte nicht geladen werden.';
        this.snackBar.open(message, 'OK', { duration: 4000 });
        // Auf das Dashboard zurück, damit keine leere Seite bleibt
        this.router.navigate(['/dashboard']);
      }
    });
  }

  private loadPdf(): void {
    console.log('=== loadPdf called ===');
    console.log('Invoice ID:', this.invoice.id);
    console.log('Full invoice object:', this.invoice);

    this.pdfLoading = true;

    // Lade PDF als Blob und konvertiere zu Uint8Array für ngx-extended-pdf-viewer
    this.invoiceService.downloadInvoicePdf(this.invoice.id).subscribe({
      next: async (blob) => {
        console.log('=== PDF download successful ===');
        console.log('Blob received, size:', blob.size, 'bytes');
        console.log('Blob type:', blob.type);

        // Konvertiere Blob zu ArrayBuffer und dann zu Uint8Array
        const arrayBuffer = await blob.arrayBuffer();
        this.pdfSrc = new Uint8Array(arrayBuffer);

        // Erstelle auch Blob URL für Fallback-Buttons
        this.pdfBlobUrl = URL.createObjectURL(blob);

        this.pdfLoading = false;
        console.log('PDF successfully loaded, pdfSrc length:', this.pdfSrc.length, 'bytes');
        console.log('pdfBlobUrl:', this.pdfBlobUrl);
      },
      error: (error) => {
        console.error('=== Error loading PDF ===');
        console.error('Error object:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Error statusText:', error.statusText);

        this.pdfSrc = null;
        this.pdfBlobUrl = null;
        this.pdfLoading = false;
        this.snackBar.open('PDF konnte nicht geladen werden: ' + (error.status || 'Unbekannter Fehler'), 'Schließen', {
          duration: 5000
        });
      }
    });
  }

  openPdfInNewTab(): void {
    if (this.pdfBlobUrl) {
      window.open(this.pdfBlobUrl, '_blank');
    }
  }

  downloadPdf(): void {
    if (this.pdfBlobUrl) {
      const link = document.createElement('a');
      link.href = this.pdfBlobUrl;
      link.download = `${this.invoice.invoiceNumber}.pdf`;
      link.click();
    }
  }

  private loadCostCenters() {
    this.costCenterService.getCostCenters().subscribe({
        next: (centers) => {
          this.costCenters = centers;
        },
        error: (error) => {
          console.error('Error loading cost centers', error);
        }
      });
  }

  private loadSuppliers() {
    this.supplierService.getSuppliers().subscribe({
        next: (suppliers) => {
          this.suppliers = suppliers;
        },
        error: (error) => {
          console.error('Error loading suppliers', error);
        }
      });
  }

  onCostCenterChange(costCenterId?: string) {
    this.invoice.costCenterId = costCenterId;
    this.invoice.projectId = undefined;
    this.loadProjects(costCenterId);
  }

  private loadProjects(costCenterId?: string) {
    if (!costCenterId) {
      this.projects = [];
      return;
    }

    this.costCenterService.getProjectsForCostCenter(costCenterId).subscribe({
        next: (projects) => {
          this.projects = projects;

          // Falls bereits eine Projekt-ID gesetzt ist, aber nicht in der Liste enthalten, füge sie als Fallback hinzu
          const currentProjectId = this.invoice.projectId;
          if (currentProjectId && !this.projects.some(p => p.id === currentProjectId)) {
            this.projects = [
              { id: currentProjectId, name: '(vorhanden)', costCenterId, costCenterName: '', budget: 0, spentAmount: 0, status: 'Aktiv' },
              ...this.projects
            ];
          }
        },
        error: (error) => {
          console.error('Error loading projects for cost center', costCenterId, error);
          // Falls Liste nicht ladbar ist, aber bereits eine Projekt-ID existiert, wenigstens diese anzeigen
          if (this.invoice.projectId) {
            this.projects = [{ id: this.invoice.projectId, name: '(vorhanden)', costCenterId, costCenterName: '', budget: 0, spentAmount: 0, status: 'Aktiv' }];
          } else {
            this.projects = [];
          }
        }
      });
  }

  private loadPurchaseOrders() {
    this.purchaseOrderService.getPurchaseOrders().subscribe({
        next: (orders) => {
          this.purchaseOrders = orders;

          // Falls bereits eine PO-ID gesetzt ist, aber nicht in der Liste, als Fallback hinzufügen
          const currentPurchaseOrderId = this.invoice.purchaseOrderId;
          if (currentPurchaseOrderId && !this.purchaseOrders.some(po => po.id === currentPurchaseOrderId)) {
            this.purchaseOrders = [
              { id: currentPurchaseOrderId, title: '(vorhanden)', totalAmount: 0, currency: 'EUR', status: 'Offen', createdAt: '' },
              ...this.purchaseOrders
            ];
          }
        },
        error: (error) => {
          console.error('Error loading purchase orders', error);
          // Falls API fehlschlägt, aber bereits eine PO-ID existiert, diese anzeigen
          if (this.invoice.purchaseOrderId) {
            this.purchaseOrders = [{ id: this.invoice.purchaseOrderId, title: '(vorhanden)', totalAmount: 0, currency: 'EUR', status: 'Offen', createdAt: '' }];
          } else {
            this.purchaseOrders = [];
          }
        }
      });
  }

  saveData() {
    if (!this.invoice || this.saving) return;

    if (!this.invoice.costCenterId) {
      this.saveStatus = 'error';
      this.saveMessage = 'Bitte Kostenstelle auswählen';
      this.snackBar.open(this.saveMessage, 'OK', { duration: 3000 });
      return;
    }

    if (!this.invoice.projectId) {
      this.saveStatus = 'error';
      this.saveMessage = 'Bitte ein Projekt auswählen';
      this.snackBar.open(this.saveMessage, 'OK', { duration: 3000 });
      return;
    }

    this.saving = true;
    const payload = {
      supplierId: this.invoice.supplier?.id,
      projectId: this.invoice.projectId?.trim() || undefined,
      costCenterId: this.invoice.costCenterId || undefined,
      purchaseOrderId: this.invoice.purchaseOrderId || undefined,
      netAmount: this.invoice.netAmount || undefined,
      taxAmount: this.invoice.taxAmount || undefined,
      totalAmount: this.invoice.totalAmount || undefined,
      invoiceDate: this.invoice.invoiceDate || undefined,
      dueDate: this.invoice.dueDate || undefined,
      description: this.invoice.description || undefined,
      internalNotes: this.invoice.internalNotes || undefined,
      requiresApproval: this.invoice.requiresApproval
    };

    this.invoiceService.updateInvoice(this.invoice.id, payload).subscribe({
      next: (updated) => {
        this.invoice = {
          ...this.invoice,
          ...updated
        };
        this.saving = false;
        this.saveStatus = 'success';
        this.lastSavedAt = new Date();
        this.saveMessage = `Daten gespeichert (Projekt: ${this.invoice.projectId || '—'})`;
        this.snackBar.open('Daten gespeichert', 'OK', { duration: 3000 });
        this.isEditMode = false;
        this.editFormDirty = false;

        // Direkt nachladen, um gespeicherte Werte aus dem Backend zu holen
        this.loadInvoice();
      },
      error: (error) => {
        console.error('Error saving invoice data:', error);
        this.saving = false;
        this.saveStatus = 'error';
        this.saveMessage = this.getErrorMessage(error) || 'Speichern fehlgeschlagen';
        this.snackBar.open(this.saveMessage, 'OK', { duration: 4000 });
      }
    });
  }

  toggleEditMode() {
    this.isEditMode = !this.isEditMode;
    this.editFormDirty = false;
    this.saveStatus = null;
  }

  cancelEdit() {
    this.isEditMode = false;
    this.editFormDirty = false;
    this.loadInvoice();
  }

  onInvoiceFieldChange() {
    this.editFormDirty = true;
    this.saveStatus = null;
  }

  private getErrorMessage(error: any): string {
    if (!error) return '';
    if (error.error?.message) return error.error.message;
    if (error.message) return error.message;
    return '';
  }

  canApprove(): boolean {
    const hasCostCenter = !!this.invoice?.costCenterId;
    const hasProject = !!this.invoice?.projectId;
    return !!this.invoice && !this.loading && hasCostCenter && hasProject;
  }

  onApprove() {
    if (!this.invoice || !this.canApprove()) {
      const msg = (!this.invoice?.costCenterId || !this.invoice?.projectId)
        ? 'Bitte Kostenstelle und Projekt ergänzen, erst dann freigeben.'
        : 'Rechnung kann nicht freigegeben werden';
      this.snackBar.open(msg, 'OK', { duration: 3000 });
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
