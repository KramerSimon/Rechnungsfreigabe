import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CostCenter } from '../../../core/models/master-data.models';

@Component({
  selector: 'app-edit-cost-center-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Kostenstelle bearbeiten</h2>

      <form [formGroup]="costCenterForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Kostenstellen-ID *</mat-label>
            <input matInput formControlName="id" readonly>
            <mat-hint>ID kann nicht geändert werden</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Name *</mat-label>
            <input matInput formControlName="name" placeholder="z.B. Marketing">
            <mat-error *ngIf="costCenterForm.get('name')?.hasError('required')">
              Name ist erforderlich
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="3"
                      placeholder="Beschreibung der Kostenstelle..."></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Budget (€)</mat-label>
            <input matInput type="number" formControlName="budget"
                   placeholder="0.00" step="0.01" min="0">
            <mat-error *ngIf="costCenterForm.get('budget')?.hasError('min')">
              Budget muss mindestens 0 sein
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Manager ID</mat-label>
            <input matInput type="number" formControlName="managerId"
                   placeholder="Optional">
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                (click)="onSave()"
                [disabled]="costCenterForm.invalid">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 500px;
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

    textarea {
      resize: vertical;
      min-height: 60px;
    }
  `]
})
export class EditCostCenterDialogComponent {
  costCenterForm: FormGroup;

  constructor(
    private dialogRef: MatDialogRef<EditCostCenterDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CostCenter,
    private fb: FormBuilder
  ) {
    this.costCenterForm = this.fb.group({
      id: [{ value: data.id || '', disabled: true }], // ID is readonly
      name: [data.name || '', [Validators.required, Validators.maxLength(100)]],
      description: [data.description || ''],
      budget: [data.budget || 0, [Validators.min(0)]],
      managerId: [null] // This would need to be mapped from managerName if needed
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.costCenterForm.valid) {
      // Don't include the disabled id field
      const formValue = { ...this.costCenterForm.value };
      delete formValue.id;
      this.dialogRef.close(formValue);
    }
  }
}
