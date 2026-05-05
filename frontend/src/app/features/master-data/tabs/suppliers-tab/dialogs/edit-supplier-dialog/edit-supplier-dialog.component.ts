import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { LanguageService } from '../../../../../../core/services/language.service';

interface Supplier {
  id: number;
  name: string;
  legalName?: string;
  taxNumber?: string;
  vatNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  postalCode?: string;
  city?: string;
  country?: string;
  email?: string;
  phone?: string;
  bankName?: string;
  iban?: string;
  bic?: string;
  paymentTermsDays?: number;
  isActive: boolean;
}

@Component({
  selector: 'app-edit-supplier-dialog',
  templateUrl: './edit-supplier-dialog.component.html',
  styleUrls: ['./edit-supplier-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    ReactiveFormsModule
  ]
})
export class EditSupplierDialogComponent {
  supplierForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<EditSupplierDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Supplier,
    private fb: FormBuilder,
    private languageService: LanguageService
  ) {
    this.supplierForm = this.fb.group({
      name: [data.name || '', [Validators.required, Validators.maxLength(100)]],
      legalName: [data.legalName || '', [Validators.maxLength(150)]],
      taxNumber: [data.taxNumber || '', [Validators.maxLength(30)]],
      vatNumber: [data.vatNumber || '', [Validators.maxLength(30)]],
      addressLine1: [data.addressLine1 || '', [Validators.maxLength(100)]],
      addressLine2: [data.addressLine2 || '', [Validators.maxLength(100)]],
      postalCode: [data.postalCode || '', [Validators.maxLength(10)]],
      city: [data.city || '', [Validators.maxLength(50)]],
      country: [data.country || 'Deutschland', [Validators.required, Validators.maxLength(50)]],
      email: [data.email || '', [Validators.email, Validators.maxLength(255)]],
      phone: [data.phone || '', [Validators.maxLength(30)]],
      bankName: [data.bankName || '', [Validators.maxLength(100)]],
      iban: [data.iban || '', [Validators.maxLength(34)]],
      bic: [data.bic || '', [Validators.maxLength(11)]],
      paymentTermsDays: [data.paymentTermsDays || null, [Validators.min(0), Validators.max(365)]]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {

    if (this.supplierForm.valid) {
      this.dialogRef.close(this.supplierForm.value);
    } else {
      // Mark all controls as touched to show validation errors
      this.supplierForm.markAllAsTouched();
      // Show an alert with missing required fields
      const missingFields = [];
      if (this.supplierForm.get('name')?.invalid) missingFields.push('Name');
      if (this.supplierForm.get('country')?.invalid) missingFields.push(this.t('md.dialog.supplier.country'));
      if (missingFields.length > 0) {
        alert(this.t('md.dialog.common.validation.requiredFields') + ' ' + missingFields.join(', '));
      }
    }
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
