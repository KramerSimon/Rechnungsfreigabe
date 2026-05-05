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
import { NgxExtendedPdfViewerModule, pdfDefaultOptions } from 'ngx-extended-pdf-viewer';
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
import { LanguageService } from '../../core/services/language.service';
import { LanguageCode } from '../../core/i18n/translations';

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
  pdfDownloadUrl: string | null = null;
  pdfSafePreviewUrl: SafeResourceUrl | null = null;
  pdfSafeUrl: SafeResourceUrl | null = null;
  pdfSrc: Uint8Array | null = null;
  pdfLoadError: string | null = null;
  useFallbackViewer = false;

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
    status: '',
    requiresApproval: true,
    approvalLevel: 1,
    autoApproved: false,
    description: '',
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
  currentUserId: number | null = null;
  userPermissions: string[] = [];
  currentUserIsAdministrator = false;
  currentLanguage: LanguageCode = 'de';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private invoiceService: InvoiceService,
    private costCenterService: CostCenterService,
    private purchaseOrderService: PurchaseOrderService,
    private supplierService: SupplierService,
    private sanitizer: DomSanitizer,
    private authService: AuthService,
    private languageService: LanguageService
  ) {}

  ngOnInit() {
    this.configurePdfViewerAssets();
    this.invoiceId = parseInt(this.route.snapshot.params['id']) || 1;
    this.authService.authState$.subscribe(authState => {
      this.currentUserId = authState?.user?.id ?? null;
      this.userPermissions = authState?.permissions || [];
      const roleNames = (authState?.user?.roles || []).map(r => (r?.name || '').toLowerCase());
      this.currentUserIsAdministrator = roleNames.includes('administrator') ||
        this.userPermissions.includes('dashboards.view_admin') ||
        this.userPermissions.includes('dashboards.view_all');
    });
    this.languageService.currentLanguage$.subscribe((language) => {
      this.currentLanguage = language;
    });
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
          ? this.t('invoice.detail.error.notFound')
          : this.t('invoice.detail.error.load');
        this.snackBar.open(message, this.t('common.close'), { duration: 4000 });
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
    this.pdfLoadError = null;
    this.useFallbackViewer = false;
    if (this.pdfBlobUrl) {
      URL.revokeObjectURL(this.pdfBlobUrl);
    }
    this.pdfBlobUrl = null;
    this.pdfDownloadUrl = null;
    this.pdfSafePreviewUrl = null;
    this.pdfSafeUrl = null;
    this.pdfSrc = null;

    // Lade PDF als Blob und konvertiere zu Uint8Array für ngx-extended-pdf-viewer
    this.invoiceService.downloadInvoicePdf(this.invoice.id).subscribe({
      next: async (blob) => {
        console.log('=== PDF download successful ===');
        console.log('Blob received, size:', blob.size, 'bytes');
        console.log('Blob type:', blob.type);

        // Konvertiere Blob zu ArrayBuffer und dann zu Uint8Array
        const arrayBuffer = await blob.arrayBuffer();
        const uint8 = new Uint8Array(arrayBuffer);
        const header = String.fromCharCode(...uint8.slice(0, 4));
        if (header !== '%PDF') {
          this.pdfSrc = null;
          this.pdfBlobUrl = null;
          this.pdfDownloadUrl = null;
          this.pdfSafePreviewUrl = null;
          this.pdfSafeUrl = null;
          this.pdfLoading = false;
          this.pdfLoadError = this.t('invoice.detail.pdf.error.invalid');
          this.useFallbackViewer = true;
          this.snackBar.open(this.t('invoice.detail.pdf.error.invalidFull'), this.t('common.close'), {
            duration: 5000
          });
          return;
        }

        this.pdfSrc = uint8;

        // Force application/pdf to maximize renderer compatibility.
        const normalizedPdfBlob = new Blob([arrayBuffer], { type: 'application/pdf' });

        // Use the validated blob as single source for preview and download.
        this.pdfBlobUrl = URL.createObjectURL(normalizedPdfBlob);
        this.pdfDownloadUrl = this.pdfBlobUrl;
        this.pdfSafePreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfBlobUrl);
        this.pdfSafeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfBlobUrl);

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
        if (this.pdfBlobUrl) {
          URL.revokeObjectURL(this.pdfBlobUrl);
        }
        this.pdfBlobUrl = null;
        this.pdfSafePreviewUrl = null;
        this.pdfSafeUrl = null;
        this.pdfDownloadUrl = null;
        this.pdfLoading = false;
        this.pdfLoadError = error?.message || this.t('common.unknownError');
        this.snackBar.open(this.tp('invoice.detail.pdf.error.load', { message: error.status || this.t('common.unknownError') }), this.t('common.close'), {
          duration: 5000
        });
      }
    });
  }

  onPdfLoadingFailed(error: any): void {
    console.error('PDF viewer failed to render:', error);
    this.pdfLoadError = error?.message || this.t('invoice.detail.pdf.previewUnavailable');
    this.useFallbackViewer = true;
  }

  onPdfLoaded(): void {
    this.useFallbackViewer = false;
    this.pdfLoadError = null;
  }

  private configurePdfViewerAssets(): void {
    pdfDefaultOptions.workerSrc = () => '/assets/pdf.worker-5.4.1105.min.mjs';
    pdfDefaultOptions.cMapUrl = () => '/assets/cmaps/';
    pdfDefaultOptions.standardFontDataUrl = () => '/assets/standard_fonts/';
    pdfDefaultOptions.sandboxBundleSrc = () => '/assets/pdf.sandbox-5.4.1105.min.mjs';
  }

  openPdfInNewTab(): void {
    if (this.pdfDownloadUrl) {
      window.open(this.pdfDownloadUrl, '_blank');
      return;
    }

    if (this.pdfBlobUrl) {
      window.open(this.pdfBlobUrl, '_blank');
    }
  }

  downloadPdf(): void {
    if (this.pdfDownloadUrl) {
      const link = document.createElement('a');
      link.href = this.pdfDownloadUrl;
      link.download = `${this.invoice.invoiceNumber}.pdf`;
      link.target = '_blank';
      link.rel = 'noopener';
      link.click();
      return;
    }

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
              { id: currentProjectId, name: this.t('invoice.detail.fallback.existing'), costCenterId, costCenterName: '', budget: 0, spentAmount: 0, status: this.t('invoice.detail.fallback.active') },
              ...this.projects
            ];
          }
        },
        error: (error) => {
          console.error('Error loading projects for cost center', costCenterId, error);
          // Falls Liste nicht ladbar ist, aber bereits eine Projekt-ID existiert, wenigstens diese anzeigen
          if (this.invoice.projectId) {
            this.projects = [{ id: this.invoice.projectId, name: this.t('invoice.detail.fallback.existing'), costCenterId, costCenterName: '', budget: 0, spentAmount: 0, status: this.t('invoice.detail.fallback.active') }];
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
              { id: currentPurchaseOrderId, title: this.t('invoice.detail.fallback.existing'), totalAmount: 0, currency: 'EUR', status: this.t('invoice.detail.fallback.open'), createdAt: '' },
              ...this.purchaseOrders
            ];
          }
        },
        error: (error) => {
          console.error('Error loading purchase orders', error);
          // Falls API fehlschlägt, aber bereits eine PO-ID existiert, diese anzeigen
          if (this.invoice.purchaseOrderId) {
            this.purchaseOrders = [{ id: this.invoice.purchaseOrderId, title: this.t('invoice.detail.fallback.existing'), totalAmount: 0, currency: 'EUR', status: this.t('invoice.detail.fallback.open'), createdAt: '' }];
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
      this.saveMessage = this.t('invoice.detail.validation.costCenterRequired');
      this.snackBar.open(this.saveMessage, this.t('common.close'), { duration: 3000 });
      return;
    }

    if (!this.invoice.projectId) {
      this.saveStatus = 'error';
      this.saveMessage = this.t('invoice.detail.validation.projectRequired');
      this.snackBar.open(this.saveMessage, this.t('common.close'), { duration: 3000 });
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
        this.saveMessage = this.tp('invoice.detail.save.savedWithProject', { project: this.invoice.projectId || this.t('md.common.notSpecified') });
        this.snackBar.open(this.t('invoice.detail.save.saved'), this.t('common.close'), { duration: 3000 });
        this.isEditMode = false;
        this.editFormDirty = false;

        // Direkt nachladen, um gespeicherte Werte aus dem Backend zu holen
        this.loadInvoice();
      },
      error: (error) => {
        console.error('Error saving invoice data:', error);
        this.saving = false;
        this.saveStatus = 'error';
        this.saveMessage = this.getErrorMessage(error) || this.t('invoice.detail.save.failed');
        this.snackBar.open(this.saveMessage, this.t('common.close'), { duration: 4000 });
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
    const hasPermission = this.currentUserIsAdministrator ||
      this.userPermissions.includes('invoices.approve') ||
      this.userPermissions.includes('invoices.approve_cost_center');
    const currentUserId = this.currentUserId != null ? Number(this.currentUserId) : null;
    const normalizeStatus = (status?: string) => (status || '')
      .trim()
      .toLowerCase()
      .replace(/[-\s]/g, '_');
    const isPendingStatus = (status?: string) => {
      const normalized = normalizeStatus(status);
      return normalized === 'pending' || normalized === 'ausstehend' || normalized === 'offen';
    };
    const isWaitingStatus = (status?: string) => {
      const normalized = normalizeStatus(status);
      return normalized === 'waiting' || normalized === 'wartend' || normalized === 'queued';
    };
    const isFinalStatus = (status?: string) => {
      const normalized = normalizeStatus(status);
      return normalized === 'approved' || normalized === 'rejected' || normalized === 'skipped';
    };
    const workflows = this.invoice?.pendingApprovals || [];
    const myWorkflows = workflows.filter(aw => Number(aw?.approverId) === currentUserId);
    const hasPendingStep = myWorkflows.some(aw => isPendingStatus(aw?.status));
    const hasEligibleWaitingStep = myWorkflows.some(aw => {
      if (!isWaitingStatus(aw?.status)) return false;

      return !workflows.some(prev => {
        const isOpen = isPendingStatus(prev?.status) || isWaitingStatus(prev?.status);
        return isOpen && (prev?.stepNumber ?? 0) < (aw?.stepNumber ?? 0);
      });
    });
    const hasEligibleLegacyOpenStep = myWorkflows.some(aw => {
      if (isPendingStatus(aw?.status) || isWaitingStatus(aw?.status) || isFinalStatus(aw?.status)) return false;
      if (aw?.approvedAt) return false;

      return !workflows.some(prev => {
        const prevStatusOpen = isPendingStatus(prev?.status) || isWaitingStatus(prev?.status);
        return prevStatusOpen && (prev?.stepNumber ?? 0) < (aw?.stepNumber ?? 0);
      });
    });

    const isPendingApprover = hasPendingStep || hasEligibleWaitingStep || hasEligibleLegacyOpenStep;

    if (this.currentUserIsAdministrator) {
      return !!this.invoice && hasCostCenter && hasProject;
    }

    return !!this.invoice && hasCostCenter && hasProject && hasPermission && isPendingApprover;
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

  onApprove() {
    if (!this.invoice || !this.canApprove()) {
      let msg = this.t('invoice.detail.approval.cannotApprove');

      if (!this.invoice?.costCenterId || !this.invoice?.projectId) {
        msg = this.t('invoice.detail.approval.needCostCenterProject');
      } else if (!this.currentUserIsAdministrator && !this.userPermissions.includes('invoices.approve') &&
        !this.userPermissions.includes('invoices.approve_cost_center')) {
        msg = this.t('invoice.detail.approval.noPermission');
      } else {
        msg = this.t('invoice.detail.approval.notAssigned');
      }
      this.snackBar.open(msg, this.t('common.close'), { duration: 3000 });
      return;
    }

    if (confirm(this.t('invoice.detail.approval.confirmApprove'))) {
      this.loading = true;

      this.invoiceService.approveInvoice(this.invoice.id, {
        approved: true,
        comments: this.note || this.t('invoice.detail.approval.defaultApproveComment')
      }).subscribe({
        next: (response) => {
          this.snackBar.open(this.t('invoice.detail.approval.approvedSuccess'), this.t('common.close'), { duration: 5000 });
          this.loading = false;
          // Zurück zum Dashboard
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('Error approving invoice:', error);
          this.snackBar.open(this.t('invoice.detail.approval.approveError'), this.t('common.close'), { duration: 5000 });
          this.loading = false;
        }
      });
    }
  }

  onReject() {
    if (!this.invoice) return;

    const reason = prompt(this.t('invoice.detail.approval.promptRejectReason'));

    if (reason && reason.trim()) {
      this.loading = true;

      this.invoiceService.approveInvoice(this.invoice.id, {
        approved: false,
        comments: reason.trim()
      }).subscribe({
        next: (response) => {
          this.snackBar.open(this.t('invoice.detail.approval.rejectedSuccess'), this.t('common.close'), { duration: 5000 });
          this.loading = false;
          // Zurück zum Dashboard
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('Error rejecting invoice:', error);
          this.snackBar.open(this.t('invoice.detail.approval.rejectError'), this.t('common.close'), { duration: 5000 });
          this.loading = false;
        }
      });
    } else if (reason !== null) {
      this.snackBar.open(this.t('invoice.detail.approval.rejectReasonRequired'), this.t('common.close'), { duration: 3000 });
    }
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }

  tp(key: string, params: Record<string, string | number>): string {
    let translated = this.t(key);
    for (const [name, value] of Object.entries(params)) {
      translated = translated.replace(`{${name}}`, String(value));
    }
    return translated;
  }

  getLocale(): string {
    switch (this.currentLanguage) {
      case 'en':
        return 'en-US';
      case 'it':
        return 'it-IT';
      default:
        return 'de-DE';
    }
  }
}
