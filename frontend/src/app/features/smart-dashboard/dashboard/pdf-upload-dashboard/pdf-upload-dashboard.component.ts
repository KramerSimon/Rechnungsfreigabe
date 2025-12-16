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
      supplierId: ['', Validators.required],
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
    if (this.selectedFiles.length === 0 || !this.uploadForm.valid) {
      return;
    }

    this.isUploading = true;
    const supplierId = this.uploadForm.get('supplierId')?.value;
    const costCenterId = this.uploadForm.get('costCenterId')?.value;
    const purchaseOrderId = this.uploadForm.get('purchaseOrderId')?.value;

    this.pdfUploadService.bulkUploadInvoicePdfs(
      this.selectedFiles,
      supplierId,
      purchaseOrderId,
      costCenterId
    ).subscribe(
      (results) => {
        this.uploadResults = results;
        this.isUploading = false;

        const message = `${results.successfulUploads.length} PDFs hochgeladen, ${results.failedUploads.length} Fehler`;
        this.snackBar.open(message, 'Schließen', { duration: 5000 });

        if (results.failedUploads.length === 0) {
          this.selectedFiles = [];
          this.uploadForm.reset();
        }

        this.loadUploadStatus();
      },
      (error) => {
        this.isUploading = false;
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
