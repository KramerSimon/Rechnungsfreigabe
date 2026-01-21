import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CostCenter } from '../../../core/models/cost-center.model';
import { Project } from '../../../core/models/project.model';
import { CreatePurchaseOrderRequest } from '../../../core/models/purchaseOrder.model';

interface CreatePurchaseOrderDialogData {
  costCenters: CostCenter[];
  projects: Project[];
}

@Component({
  selector: 'app-create-purchase-order-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Neue Bestellung</h2>

      <form [formGroup]="form" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Bestell-ID *</mat-label>
            <input matInput formControlName="id" placeholder="z.B. PO-2026-001">
            <mat-error *ngIf="form.get('id')?.hasError('required')">
              ID ist erforderlich
            </mat-error>
            <mat-error *ngIf="form.get('id')?.hasError('maxlength')">
              Maximal 20 Zeichen
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Titel *</mat-label>
            <input matInput formControlName="title" placeholder="z.B. Hardware-Einkauf">
            <mat-error *ngIf="form.get('title')?.hasError('required')">
              Titel ist erforderlich
            </mat-error>
            <mat-error *ngIf="form.get('title')?.hasError('maxlength')">
              Maximal 100 Zeichen
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="3" placeholder="Details zur Bestellung"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Kostenstelle</mat-label>
            <mat-select formControlName="costCenterId">
              <mat-option [value]="''">Keine</mat-option>
              <mat-option *ngFor="let cc of data.costCenters" [value]="cc.id">
                {{cc.id}} - {{cc.name}}
              </mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Projekt</mat-label>
            <mat-select formControlName="projectId">
              <mat-option [value]="''">Kein Projekt</mat-option>
              <mat-option *ngFor="let project of data.projects" [value]="project.id">
                {{project.id}} - {{project.name}}
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Betrag *</mat-label>
            <input matInput type="number" formControlName="totalAmount" min="0.01" step="0.01">
            <mat-error *ngIf="form.get('totalAmount')?.hasError('required')">
              Betrag ist erforderlich
            </mat-error>
            <mat-error *ngIf="form.get('totalAmount')?.hasError('min')">
              Betrag muss größer als 0 sein
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Währung</mat-label>
            <input matInput formControlName="currency" maxlength="3" placeholder="EUR">
            <mat-error *ngIf="form.get('currency')?.hasError('maxlength')">
              Maximal 3 Zeichen
            </mat-error>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary" (click)="onSave()" [disabled]="form.invalid">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 640px;
      max-width: 92vw;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 12px;
    }

    .form-row mat-form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    textarea {
      resize: vertical;
      min-height: 64px;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 12px 0 4px;
    }
  `],
})
export class CreatePurchaseOrderDialogComponent {
  form: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<CreatePurchaseOrderDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreatePurchaseOrderDialogData,
    private fb: FormBuilder
  ) {
    this.form = this.fb.group({
      id: ['', [Validators.required, Validators.maxLength(20)]],
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: [''],
      costCenterId: [''],
      projectId: [''],
      totalAmount: [0, [Validators.required, Validators.min(0.01)]],
      currency: ['EUR', [Validators.maxLength(3)]],
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.invalid) {
      return;
    }

    const value = this.form.value as CreatePurchaseOrderRequest;
    const payload: CreatePurchaseOrderRequest = {
      ...value,
      currency: (value.currency || 'EUR').toUpperCase(),
      costCenterId: value.costCenterId || undefined,
      projectId: value.projectId || undefined,
    };

    this.dialogRef.close(payload);
  }
}
