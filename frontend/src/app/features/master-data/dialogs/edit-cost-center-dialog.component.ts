import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CostCenter } from '../../../core/models/cost-center.model';
import { User } from '../../../core/models/user.models';

@Component({
  selector: 'app-edit-cost-center-dialog',
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
            <mat-label>Manager</mat-label>
            <mat-select formControlName="managerId">
              <mat-option value="null">Kein Manager</mat-option>
              <mat-option *ngFor="let user of managers" [value]="user.id">
                {{user.firstName}} {{user.lastName}} ({{user.username}})
              </mat-option>
            </mat-select>
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
      max-width: 95vw;
      overflow-x: hidden;
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
  managers: User[] = [];

  constructor(
    private dialogRef: MatDialogRef<EditCostCenterDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { costCenter: CostCenter; managers: User[] },
    private fb: FormBuilder
  ) {
    this.managers = data.managers || [];
    const costCenter = data.costCenter || {};
    this.costCenterForm = this.fb.group({
      id: [{ value: costCenter.id || '', disabled: true }],
      name: [costCenter.name || '', [Validators.required, Validators.maxLength(100)]],
      description: [costCenter.description || ''],
      budget: [costCenter.budget || 0, [Validators.min(0)]],
      managerId: [costCenter.managerId || null]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.costCenterForm.valid) {
      const formValue = { ...this.costCenterForm.getRawValue() };
      this.dialogRef.close(formValue);
    }
  }
}
