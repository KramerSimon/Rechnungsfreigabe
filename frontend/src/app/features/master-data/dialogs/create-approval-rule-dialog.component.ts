import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CreateApprovalRuleDto } from '../../../core/models/approval.model';

@Component({
  selector: 'app-create-approval-rule-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Neue Genehmigungsregel</h2>

      <form [formGroup]="ruleForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Name *</mat-label>
            <input matInput formControlName="name" required>
            <mat-error *ngIf="ruleForm.get('name')?.hasError('required')">
              Name ist erforderlich
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="3"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Regeltyp *</mat-label>
            <mat-select formControlName="ruleType" required>
              <mat-option value="Manual">Manuell</mat-option>
              <mat-option value="Automatic">Automatisch</mat-option>
              <mat-option value="Conditional">Bedingt</mat-option>
            </mat-select>
            <mat-error *ngIf="ruleForm.get('ruleType')?.hasError('required')">
              Regeltyp ist erforderlich
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Priorität</mat-label>
            <input matInput type="number" formControlName="priority" min="1" max="100">
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                [disabled]="!ruleForm.valid"
                (click)="onSave()">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      padding: 20px;
      min-width: 400px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }

    mat-form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    [mat-dialog-actions] {
      margin-top: 24px;
    }
  `]
})
export class CreateApprovalRuleDialogComponent {
  ruleForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<CreateApprovalRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateApprovalRuleDto
  ) {
    this.ruleForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      ruleType: ['Manual', Validators.required],
      priority: [10],
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.ruleForm.valid) {
      this.dialogRef.close(this.ruleForm.value);
    }
  }
}
