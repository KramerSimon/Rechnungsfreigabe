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

  uploadForm!: FormGroup;
  selectedFiles: File[] = [];
  isDragOver = false;
  isUploading = false;
  suppliers: any[] = [];
  costCenters: any[] = [];
  uploadResults: any = null;
  uploadStatus: any = null;

  // Progress tracking
  uploadProgress = 0;
  currentFileProgress = 0;
  currentFileName = '';
  uploadPhase = ''; // 'uploading', 'processing', 'complete'
  fileProgressDetails: any[] = []; // Array to track progress of each file

  displayedSuccessColumns = ['fileName', 'invoiceNumber', 'invoiceId', 'size'];
  displayedErrorColumns = ['fileName', 'errorMessage'];

  constructor(
    private fb: FormBuilder,
    private pdfUploadService: PdfUploadService,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private snackBar: MatSnackBar
  ) {
    this.uploadForm = this.fb.group({
      supplierId: [''],  // Optional - wird aus PDF extrahiert wenn nicht angegeben
      costCenterId: [''],
      purchaseOrderId: ['']
    });
  }

  ngOnInit() {
    this.loadSuppliers();
    this.loadCostCenters();
    this.loadUploadStatus();
  }

  loadSuppliers() {
    this.supplierService.getSuppliers().subscribe(
      (suppliers) => {
        this.suppliers = suppliers;
      },
      (error) => {
        this.snackBar.open('Fehler beim Laden der Lieferanten', 'Schließen', { duration: 3000 });
      }
    );
  }

  loadCostCenters() {
    this.costCenterService.getCostCenters().subscribe(
      (costCenters) => {
        this.costCenters = costCenters;
      },
      (error) => {
        this.snackBar.open('Fehler beim Laden der Kostenstellen', 'Schließen', { duration: 3000 });
      }
    );
  }

  loadUploadStatus() {
    this.pdfUploadService.getUploadStatus().subscribe(
      (status) => {
        this.uploadStatus = status;
      },
      (error) => {
        console.error('Error loading upload status:', error);
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
      if (file.type === 'application/pdf' || file.name.endsWith('.pdf')) {
        if (!this.selectedFiles.find(f => f.name === file.name)) {
          this.selectedFiles.push(file);
        }
      } else {
        this.snackBar.open(`${file.name} ist keine PDF-Datei`, 'Schließen', { duration: 3000 });
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
      costCenterId
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

        let message = `${results.successfulUploads.length} PDFs hochgeladen, ${results.failedUploads.length} Fehler`;
        if (hasWarnings) {
          message += ' ⚠️ Einige PDFs enthalten keine extrahierbaren Daten (gescannte Dokumente)';
        }

        this.snackBar.open(message, 'Schließen', { duration: 8000 });

        if (hasWarnings) {
          // Show additional warning
          setTimeout(() => {
            this.snackBar.open(
              'Bitte Rechnungsdaten für gescannte PDFs manuell vervollständigen',
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
        this.snackBar.open('Fehler beim Upload: ' + (error.message || 'Unbekannter Fehler'), 'Schließen', { duration: 5000 });
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
}
