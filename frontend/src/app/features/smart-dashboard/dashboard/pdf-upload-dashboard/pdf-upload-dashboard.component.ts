import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PdfUploadService } from '../../../../core/services/pdf-upload.service';
import { SupplierService } from '../../../../core/services/supplier.service';
import { CostCenterService } from '../../../../core/services/cost-center.service';
import { ProjectService } from '../../../../core/services/project.service';
import { PurchaseOrderService } from '../../../../core/services/purchase-order.service';
import { LanguageService } from '../../../../core/services/language.service';

interface UploadedFile {
  fileName: string;
  invoiceId: number;
  invoiceNumber: string;
  size: number;
  uploadedAt?: Date;
}

interface FailedFile {
  fileName: string;
  errorMessage: string;
}

@Component({
  selector: 'app-pdf-upload-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: `./pdf-upload-dashboard.component.html`,
  styleUrls: [`./pdf-upload-dashboard.component.scss`],
})
export class PdfUploadDashboardComponent implements OnInit {
  @ViewChild('fileInput') fileInput: any;
  @ViewChild('poFileInput') poFileInput: any;

  uploadForm!: FormGroup;
  poUploadForm!: FormGroup;
  selectedFiles: File[] = [];
  selectedPoFile: File | null = null;
  isDragOver = false;
  isPoDropZoneActive = false;
  isUploading = false;
  isPoUploading = false;
  suppliers: any[] = [];
  costCenters: any[] = [];
  projects: any[] = [];
  purchaseOrders: any[] = [];
  filteredProjects: any[] = [];  // Projects filtered by selected cost center
  filteredProjectsForInvoice: any[] = [];  // Projects filtered for invoice upload
  filteredPurchaseOrders: any[] = [];  // Purchase orders filtered by cost center and project
  uploadResults: any = null;
  uploadStatus: any = null;

  // Progress tracking for invoices
  uploadProgress = 0;
  currentFileProgress = 0;
  currentFileName = '';
  uploadPhase = ''; // 'uploading', 'processing', 'complete'
  fileProgressDetails: any[] = []; // Array to track progress of each file

  // Progress tracking for purchase orders
  poUploadProgress = 0;
  poUploadPhase = '';

  displayedSuccessColumns = ['fileName', 'invoiceNumber', 'invoiceId', 'size'];
  displayedErrorColumns = ['fileName', 'errorMessage'];

  constructor(
    private fb: FormBuilder,
    private pdfUploadService: PdfUploadService,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService,
    private purchaseOrderService: PurchaseOrderService,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {
    this.uploadForm = this.fb.group({
      supplierId: [''],  // Optional - wird aus PDF extrahiert wenn nicht angegeben
      costCenterId: [''],
      projectId: [''],  // Optional - zum Zuordnen des Projekts
      purchaseOrderId: ['']
    });
    this.poUploadForm = this.fb.group({
      supplierId: [''],  // Optional - wird aus PDF extrahiert oder neu erstellt
      costCenterId: ['', Validators.required],  // REQUIRED
      projectId: ['', Validators.required]  // REQUIRED
    });

    // Listen for cost center changes to filter projects in invoice upload
    this.uploadForm.get('costCenterId')?.valueChanges.subscribe((costCenterId) => {
      this.onInvoiceCostCenterChanged(costCenterId);
    });

    // Listen for project changes to filter purchase orders in invoice upload
    this.uploadForm.get('projectId')?.valueChanges.subscribe((projectId) => {
      this.onInvoiceProjectChanged(projectId);
    });

    // Listen for cost center changes to filter projects in PO upload
    this.poUploadForm.get('costCenterId')?.valueChanges.subscribe((costCenterId) => {
      this.onCostCenterChanged(costCenterId);
    });
  }

  ngOnInit() {
    this.loadSuppliers();
    this.loadCostCenters();
    this.loadProjects();
      this.loadPurchaseOrders();
    this.loadUploadStatus();
  }

  loadSuppliers() {
    this.supplierService.getSuppliers().subscribe(
      (suppliers) => {
        this.suppliers = suppliers;
      },
      (error) => {
        this.snackBar.open(this.t('dashboard.pdf.error.loadSuppliers'), this.t('common.close'), { duration: 3000 });
      }
    );
  }

  loadCostCenters() {
    this.costCenterService.getCostCenters().subscribe(
      (costCenters) => {
        this.costCenters = costCenters;
      },
      (error) => {
        this.snackBar.open(this.t('dashboard.pdf.error.loadCostCenters'), this.t('common.close'), { duration: 3000 });
      }
    );
  }

  loadProjects() {
    this.projectService.getProjects().subscribe(
      (projects) => {
        this.projects = projects;
        // Initialize filtered projects
        this.filteredProjects = this.projects;
      },
      (error) => {
        this.snackBar.open(this.t('dashboard.pdf.error.loadProjects'), this.t('common.close'), { duration: 3000 });
      }
    );
  }

  loadPurchaseOrders() {
    this.purchaseOrderService.getPurchaseOrders().subscribe(
      (purchaseOrders) => {
        this.purchaseOrders = purchaseOrders;
        this.filteredPurchaseOrders = this.purchaseOrders;
      },
      (error) => {
        this.snackBar.open(this.t('dashboard.pdf.error.loadOrders'), this.t('common.close'), { duration: 3000 });
      }
    );
  }

  onInvoiceCostCenterChanged(costCenterId: string) {
    // Reset project selection when cost center changes
    this.uploadForm.get('projectId')?.reset('', { emitEvent: false });
    // Reset purchase order selection when cost center changes
    this.uploadForm.get('purchaseOrderId')?.reset('', { emitEvent: false });

    if (!costCenterId) {
      this.filteredProjectsForInvoice = [];
      this.filteredPurchaseOrders = [];
    } else {
      // Filter projects by selected cost center
      this.filteredProjectsForInvoice = this.projects.filter(p => p.costCenterId === costCenterId);
      // Filter purchase orders by selected cost center
      this.filteredPurchaseOrders = this.purchaseOrders.filter(po => po.costCenterId === costCenterId);
    }
  }

  onInvoiceProjectChanged(projectId: string) {
    // Reset purchase order selection when project changes
    this.uploadForm.get('purchaseOrderId')?.reset('', { emitEvent: false });

    const costCenterId = this.uploadForm.get('costCenterId')?.value;

    if (!projectId || !costCenterId) {
      // Only show purchase orders for the cost center
      this.filteredPurchaseOrders = this.purchaseOrders.filter(po => po.costCenterId === costCenterId);
    } else {
      // Filter purchase orders by both cost center and project
      this.filteredPurchaseOrders = this.purchaseOrders.filter(po =>
        po.costCenterId === costCenterId && po.projectId === projectId
      );
    }
  }

  onCostCenterChanged(costCenterId: string) {
    // Reset project selection when cost center changes
    this.poUploadForm.get('projectId')?.reset('', { emitEvent: false });

    if (!costCenterId) {
      this.filteredProjects = [];
    } else {
      // Filter projects by selected cost center
      this.filteredProjects = this.projects.filter(p => p.costCenterId === costCenterId);
    }
  }

  loadUploadStatus() {
    this.pdfUploadService.getUploadStatus().subscribe(
      (status) => {
        this.uploadStatus = status;
      },
      (error) => {
        console.error('Error loading upload status:', error);
        this.snackBar.open(this.t('dashboard.pdf.error.loadUploadStatus'), this.t('common.close'), { duration: 3000 });
      }
    );
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onFileDropped(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files) {
      this.addFiles(files);
    }
  }

  onFileSelected(event: any) {
    const files = event.target.files;
    if (files) {
      this.addFiles(files);
    }
  }

  addFiles(files: FileList) {
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      if (this.isPdfOrXml(file)) {
        if (!this.selectedFiles.find(f => f.name === file.name)) {
          this.selectedFiles.push(file);
        }
      } else {
        this.snackBar.open(
          this.tp('dashboard.pdf.error.invalidType', { file: file.name }),
          this.t('common.close'),
          { duration: 3000 }
        );
      }
    }
  }

  removeFile(file: File) {
    this.selectedFiles = this.selectedFiles.filter(f => f !== file);
  }

  clearSelection() {
    this.selectedFiles = [];
  }

  uploadFiles() {
    if (this.selectedFiles.length === 0) {
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadPhase = 'uploading';
    this.fileProgressDetails = this.selectedFiles.map(f => ({
      name: f.name,
      progress: 0,
      status: 'pending' // pending, uploading, processing, complete, error
    }));

    const supplierIdValue = this.uploadForm.get('supplierId')?.value;
    const supplierId = supplierIdValue && supplierIdValue !== '' ? Number(supplierIdValue) : undefined;
    const costCenterIdValue = this.uploadForm.get('costCenterId')?.value;
    const costCenterId = costCenterIdValue && costCenterIdValue !== '' ? costCenterIdValue : undefined;
    const projectIdValue = this.uploadForm.get('projectId')?.value;
    const projectId = projectIdValue && projectIdValue !== '' ? projectIdValue : undefined;
    const purchaseOrderIdValue = this.uploadForm.get('purchaseOrderId')?.value;
    const purchaseOrderId = purchaseOrderIdValue && purchaseOrderIdValue !== '' ? purchaseOrderIdValue : undefined;

    const fileCount = this.selectedFiles.length;
    let currentFileIndex = 0;

    // Detaillierte Progress-Simulation mit realistischen Phasen
    const progressInterval = setInterval(() => {
      const progressPerFile = 100 / fileCount;
      const baseProgress = currentFileIndex * progressPerFile;

      // Phase 1: Upload (0-20% pro Datei)
      if (this.uploadPhase === 'uploading' && this.uploadProgress < baseProgress + progressPerFile * 0.2) {
        this.uploadProgress += Math.random() * 3;

        // Aktualisiere aktuelles File
        if (this.fileProgressDetails[currentFileIndex]) {
          this.fileProgressDetails[currentFileIndex].status = 'uploading';
          this.fileProgressDetails[currentFileIndex].progress = Math.min(
            ((this.uploadProgress - baseProgress) / (progressPerFile * 0.2)) * 20,
            20
          );
        }
      }
      // Phase 2: OCR Processing (20-80% pro Datei)
      else if (this.uploadProgress >= baseProgress + progressPerFile * 0.2 &&
               this.uploadProgress < baseProgress + progressPerFile * 0.8) {
        this.uploadPhase = 'processing';
        this.uploadProgress += Math.random() * 2;

        if (this.fileProgressDetails[currentFileIndex]) {
          this.fileProgressDetails[currentFileIndex].status = 'processing';
          const ocrProgress = ((this.uploadProgress - baseProgress - progressPerFile * 0.2) /
                              (progressPerFile * 0.6)) * 60;
          this.fileProgressDetails[currentFileIndex].progress = Math.min(20 + ocrProgress, 80);
        }
      }
      // Phase 3: Saving (80-100% pro Datei)
      else if (this.uploadProgress >= baseProgress + progressPerFile * 0.8 &&
               this.uploadProgress < baseProgress + progressPerFile) {
        this.uploadPhase = 'saving';
        this.uploadProgress += Math.random() * 4;

        if (this.fileProgressDetails[currentFileIndex]) {
          const saveProgress = ((this.uploadProgress - baseProgress - progressPerFile * 0.8) /
                               (progressPerFile * 0.2)) * 20;
          this.fileProgressDetails[currentFileIndex].progress = Math.min(80 + saveProgress, 100);
        }
      }
      // Nächste Datei
      else if (this.uploadProgress >= baseProgress + progressPerFile && currentFileIndex < fileCount - 1) {
        if (this.fileProgressDetails[currentFileIndex]) {
          this.fileProgressDetails[currentFileIndex].status = 'complete';
          this.fileProgressDetails[currentFileIndex].progress = 100;
        }
        currentFileIndex++;
        this.uploadPhase = 'uploading';
      }

      this.uploadProgress = Math.min(this.uploadProgress, 95);
    }, 150);

    this.pdfUploadService.bulkUploadInvoicePdfs(
      this.selectedFiles,
      supplierId,
      purchaseOrderId,
      costCenterId,
      projectId
    ).subscribe(
      (results) => {
        clearInterval(progressInterval);
        this.uploadProgress = 100;
        this.uploadPhase = 'complete';
        this.uploadResults = results;
        this.isUploading = false;

        // Mark files as complete
        this.fileProgressDetails.forEach(fp => {
          if (fp.status === 'pending' || fp.status === 'uploading' || fp.status === 'processing') {
            fp.status = 'complete';
            fp.progress = 100;
          }
        });

        // Scroll to results after a short delay
        setTimeout(() => {
          const resultsElement = document.querySelector('.results-section');
          if (resultsElement) {
            resultsElement.scrollIntoView({ behavior: 'smooth' });
          }
        }, 500);

        // Check for warnings about missing data extraction
        const hasWarnings = results.successfulUploads.some((upload: any) => upload.warning || upload.requiresManualEntry);

        let message = this.tp('dashboard.pdf.upload.summary', {
          success: results.successfulUploads.length,
          failed: results.failedUploads.length
        });
        if (hasWarnings) {
          message += ` ${this.t('dashboard.pdf.upload.warningScanned')}`;
        }

        this.snackBar.open(message, this.t('common.close'), { duration: 8000 });

        if (hasWarnings) {
          // Show additional warning
          setTimeout(() => {
            this.snackBar.open(
              this.t('dashboard.pdf.upload.completeInvoiceData'),
              'OK',
              { duration: 10000 }
            );
          }, 1000);
        }

        if (results.failedUploads.length === 0) {
          this.selectedFiles = [];
          this.uploadForm.reset();
          // Reset progress after 2 seconds
          setTimeout(() => {
            this.uploadProgress = 0;
            this.uploadPhase = '';
            this.fileProgressDetails = [];
          }, 2000);
        }

        this.loadUploadStatus();
      },
      (error) => {
        clearInterval(progressInterval);
        this.isUploading = false;
        this.uploadPhase = '';
        this.snackBar.open(
          this.tp('dashboard.pdf.error.uploadFailed', { message: error.message || this.t('common.unknownError') }),
          this.t('common.close'),
          { duration: 5000 }
        );
      }
    );
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  getPercentage(value: number, total: number): number {
    return total === 0 ? 0 : Math.round((value / total) * 100);
  }

  // Purchase Order Upload Methods
  onPoDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isPoDropZoneActive = true;
  }

  onPoDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isPoDropZoneActive = false;
  }

  onPoFileDropped(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isPoDropZoneActive = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      const file = files[0];
      if (this.isPdfOrXml(file)) {
        this.selectedPoFile = file;
      } else {
        this.snackBar.open(
          this.tp('dashboard.pdf.error.invalidType', { file: file.name }),
          this.t('common.close'),
          { duration: 3000 }
        );
      }
    }
  }

  onPoFileSelected(event: any) {
    const files = event.target.files;
    if (files && files.length > 0) {
      const file = files[0];
      if (this.isPdfOrXml(file)) {
        this.selectedPoFile = file;
      } else {
        this.snackBar.open(
          this.tp('dashboard.pdf.error.invalidType', { file: file.name }),
          this.t('common.close'),
          { duration: 3000 }
        );
      }
    }
  }

  private isPdfOrXml(file: File): boolean {
    const fileName = file.name.toLowerCase();
    return file.type === 'application/pdf'
      || file.type === 'application/xml'
      || file.type === 'text/xml'
      || fileName.endsWith('.pdf')
      || fileName.endsWith('.xml');
  }

  removePoFile() {
    this.selectedPoFile = null;
  }

  clearPoSelection() {
    this.selectedPoFile = null;
    this.poUploadForm.reset();
  }

  uploadPurchaseOrder() {
    if (!this.selectedPoFile || !this.poUploadForm.valid) {
      if (!this.poUploadForm.get('costCenterId')?.value) {
        this.snackBar.open(this.t('dashboard.pdf.error.costCenterRequired'), this.t('common.close'), { duration: 3000 });
      } else if (!this.poUploadForm.get('projectId')?.value) {
        this.snackBar.open(this.t('dashboard.pdf.error.projectRequired'), this.t('common.close'), { duration: 3000 });
      }
      return;
    }

    this.isPoUploading = true;
    this.poUploadProgress = 0;
    this.poUploadPhase = 'uploading';

    const supplierIdValue = this.poUploadForm.get('supplierId')?.value;
    const supplierId = supplierIdValue && supplierIdValue !== '' ? Number(supplierIdValue) : undefined;
    const costCenterIdValue = this.poUploadForm.get('costCenterId')?.value;
    const costCenterId = costCenterIdValue && costCenterIdValue !== '' ? costCenterIdValue : undefined;
    const projectIdValue = this.poUploadForm.get('projectId')?.value;
    const projectId = projectIdValue && projectIdValue !== '' ? projectIdValue : undefined;

    // Progress simulation
    const progressInterval = setInterval(() => {
      if (this.poUploadPhase === 'uploading' && this.poUploadProgress < 20) {
        this.poUploadProgress += Math.random() * 3;
      } else if (this.poUploadProgress >= 20 && this.poUploadProgress < 80) {
        this.poUploadPhase = 'processing';
        this.poUploadProgress += Math.random() * 2;
      } else if (this.poUploadProgress >= 80 && this.poUploadProgress < 95) {
        this.poUploadPhase = 'saving';
        this.poUploadProgress += Math.random() * 4;
      }
      this.poUploadProgress = Math.min(this.poUploadProgress, 95);
    }, 150);

    this.pdfUploadService.uploadPurchaseOrderPdf(
      this.selectedPoFile,
      supplierId,
      costCenterId,
      projectId
    ).subscribe(
      (result) => {
        clearInterval(progressInterval);
        this.poUploadProgress = 100;
        this.poUploadPhase = 'complete';
        this.isPoUploading = false;

        const hasWarning = result.warning || result.requiresManualEntry;
        let message = this.t('dashboard.pdf.po.success');
        if (hasWarning) {
          message += ` ${this.t('dashboard.pdf.po.warningScanned')}`;
        }

        this.snackBar.open(message, this.t('common.close'), { duration: hasWarning ? 10000 : 5000 });

        // Reset form after success
        setTimeout(() => {
          this.selectedPoFile = null;
          this.poUploadForm.reset();
          this.poUploadProgress = 0;
          this.poUploadPhase = '';
        }, 2000);

        this.loadUploadStatus();
      },
      (error) => {
        clearInterval(progressInterval);
        this.isPoUploading = false;
        this.poUploadPhase = '';
        this.snackBar.open(
          this.tp('dashboard.pdf.error.uploadFailed', { message: error.message || this.t('common.unknownError') }),
          this.t('common.close'),
          { duration: 5000 }
        );
      }
    );
  }

  getUploadButtonLabel(): string {
    return this.isUploading
      ? this.t('dashboard.pdf.upload.inProgress')
      : this.tp('dashboard.pdf.upload.startWithCount', { count: this.selectedFiles.length });
  }

  getPoUploadButtonLabel(): string {
    return this.isPoUploading
      ? this.t('dashboard.pdf.upload.inProgress')
      : this.t('dashboard.pdf.po.uploadButton');
  }

  getUploadPhaseLabel(): string {
    switch (this.uploadPhase) {
      case 'uploading':
        return this.t('dashboard.pdf.phase.uploading');
      case 'processing':
        return this.t('dashboard.pdf.phase.processing');
      case 'saving':
        return this.t('dashboard.pdf.phase.savingInvoices');
      case 'complete':
        return this.t('dashboard.pdf.phase.complete');
      default:
        return '';
    }
  }

  getPoUploadPhaseLabel(): string {
    switch (this.poUploadPhase) {
      case 'uploading':
        return this.t('dashboard.pdf.phase.uploadingSingle');
      case 'processing':
        return this.t('dashboard.pdf.phase.processing');
      case 'saving':
        return this.t('dashboard.pdf.phase.savingOrders');
      case 'complete':
        return this.t('dashboard.pdf.phase.complete');
      default:
        return '';
    }
  }

  getFileStatusLabel(status: string): string {
    switch (status) {
      case 'uploading':
        return this.t('dashboard.pdf.fileStatus.uploading');
      case 'processing':
        return this.t('dashboard.pdf.fileStatus.processing');
      case 'complete':
        return this.t('dashboard.pdf.fileStatus.complete');
      default:
        return this.t('dashboard.pdf.fileStatus.pending');
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
}
