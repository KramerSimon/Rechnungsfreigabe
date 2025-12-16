import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

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
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Lieferant bearbeiten</h2>

      <form [formGroup]="supplierForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Name *</mat-label>
            <input matInput formControlName="name" required>
            <mat-error *ngIf="supplierForm.get('name')?.hasError('required')">
              Name ist erforderlich
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Firmenname</mat-label>
            <input matInput formControlName="legalName">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Steuernummer</mat-label>
            <input matInput formControlName="taxNumber">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>USt-IdNr.</mat-label>
            <input matInput formControlName="vatNumber">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Adresse</mat-label>
            <input matInput formControlName="addressLine1">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Adresse 2</mat-label>
            <input matInput formControlName="addressLine2">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>PLZ</mat-label>
            <input matInput formControlName="postalCode">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Stadt</mat-label>
            <input matInput formControlName="city">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Land</mat-label>
            <mat-select formControlName="country">
              <mat-option value="Deutschland">Deutschland</mat-option>
              <mat-option value="Österreich">Österreich</mat-option>
              <mat-option value="Schweiz">Schweiz</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>E-Mail</mat-label>
            <input matInput type="email" formControlName="email">
            <mat-error *ngIf="supplierForm.get('email')?.hasError('email')">
              Ungültige E-Mail-Adresse
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Telefon</mat-label>
            <input matInput formControlName="phone">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Bankname</mat-label>
            <input matInput formControlName="bankName">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>IBAN</mat-label>
            <input matInput formControlName="iban">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>BIC</mat-label>
            <input matInput formControlName="bic">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Zahlungsziel (Tage)</mat-label>
            <input matInput type="number" formControlName="paymentTermsDays">
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                (click)="onSave()"
                [disabled]="supplierForm.invalid">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 600px;
      max-width: 90vw;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 8px;
    }

    .form-row mat-form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 16px 0;
    }
  `]
})
export class EditSupplierDialogComponent {
  supplierForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<EditSupplierDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Supplier,
    private fb: FormBuilder
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
      if (this.supplierForm.get('country')?.invalid) missingFields.push('Land');
      if (missingFields.length > 0) {
        alert('Bitte füllen Sie die folgenden Pflichtfelder aus: ' + missingFields.join(', '));
      }
    }
  }
}
