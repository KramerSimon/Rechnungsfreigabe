import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CostCenter } from '../../../core/models/cost-center.model';
import { Project } from '../../../core/models/project.model';


@Component({
  selector: 'app-edit-project-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Projekt bearbeiten</h2>

      <form [formGroup]="projectForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Projekt-ID *</mat-label>
            <input matInput formControlName="id" readonly>
            <mat-hint>ID kann nicht geändert werden</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Projektname *</mat-label>
            <input matInput formControlName="name" placeholder="z.B. Website Relaunch">
            <mat-error *ngIf="projectForm.get('name')?.hasError('required')">
              Name ist erforderlich
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="3"
                      placeholder="Projektbeschreibung..."></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Kostenstelle *</mat-label>
            <mat-select formControlName="costCenterId">
              <mat-option *ngFor="let cc of costCenters" [value]="cc.id">
                {{cc.id}} - {{cc.name}}
              </mat-option>
            </mat-select>
            <mat-error *ngIf="projectForm.get('costCenterId')?.hasError('required')">
              Kostenstelle ist erforderlich
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              <mat-select-trigger>
                <span class="status-tag" [style.backgroundColor]="getProjectStatusMeta(projectForm.get('status')?.value).color">
                  {{getProjectStatusMeta(projectForm.get('status')?.value).label}}
                </span>
              </mat-select-trigger>
              <mat-option value="Geplant">
                <span class="status-tag" style="background-color: #2196F3;">
                  Geplant
                </span>
              </mat-option>
              <mat-option value="Aktiv">
                <span class="status-tag" style="background-color: #4CAF50;">
                  Aktiv
                </span>
              </mat-option>
              <mat-option value="Pausiert">
                <span class="status-tag" style="background-color: #FFA500;">
                  Pausiert
                </span>
              </mat-option>
              <mat-option value="Abgeschlossen">
                <span class="status-tag" style="background-color: #9E9E9E;">
                  Abgeschlossen
                </span>
              </mat-option>
              <mat-option value="Abgebrochen">
                <span class="status-tag" style="background-color: #F44336;">
                  Abgebrochen
                </span>
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Budget (€)</mat-label>
            <input matInput type="number" formControlName="budget"
                   placeholder="0.00" step="0.01" min="0">
            <mat-error *ngIf="projectForm.get('budget')?.hasError('min')">
              Budget muss mindestens 0 sein
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Projektmanager ID</mat-label>
            <input matInput type="number" formControlName="projectManagerId"
                   placeholder="Optional">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Startdatum</mat-label>
            <input matInput [matDatepicker]="startPicker" formControlName="startDate">
            <mat-hint>MM/TT/JJJJ</mat-hint>
            <mat-datepicker-toggle matIconSuffix [for]="startPicker"></mat-datepicker-toggle>
            <mat-datepicker #startPicker></mat-datepicker>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Enddatum</mat-label>
            <input matInput [matDatepicker]="endPicker" formControlName="endDate">
            <mat-hint>MM/TT/JJJJ</mat-hint>
            <mat-datepicker-toggle matIconSuffix [for]="endPicker"></mat-datepicker-toggle>
            <mat-datepicker #endPicker></mat-datepicker>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                (click)="onSave()"
                [disabled]="projectForm.invalid">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 600px;
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

    /* Status tags in dropdown */
    .status-tag {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 13px;
      font-weight: 500;
      color: white;
      white-space: nowrap;
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
export class EditProjectDialogComponent {
  projectForm: FormGroup;
  costCenters: CostCenter[] = [];

  constructor(
    private dialogRef: MatDialogRef<EditProjectDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { project: Project, costCenters: CostCenter[] },
    private fb: FormBuilder
  ) {
    this.costCenters = data.costCenters;
    const project = data.project;

    this.projectForm = this.fb.group({
      id: [{ value: project.id || '', disabled: true }], // ID is readonly
      name: [project.name || '', [Validators.required, Validators.maxLength(100)]],
      description: [project.description || ''],
      costCenterId: [project.costCenterId || '', [Validators.required]],
      budget: [project.budget || 0, [Validators.min(0)]],
      status: [project.status || 'Geplant'],
      startDate: [project.startDate ? new Date(project.startDate) : null],
      endDate: [project.endDate ? new Date(project.endDate) : null],
      projectManagerId: [null] // This would need to be mapped from project manager if needed
    });
  }

  getProjectStatusMeta(status?: string | null): { label: string; color: string } {
    switch (status) {
      case 'Geplant':
        return { label: 'Geplant', color: '#2196F3' };
      case 'Aktiv':
        return { label: 'Aktiv', color: '#4CAF50' };
      case 'Pausiert':
        return { label: 'Pausiert', color: '#FFA500' };
      case 'Abgeschlossen':
        return { label: 'Abgeschlossen', color: '#9E9E9E' };
      case 'Abgebrochen':
        return { label: 'Abgebrochen', color: '#F44336' };
      default:
        return { label: '—', color: '#9E9E9E' };
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.projectForm.valid) {
      // Format dates properly and exclude disabled fields
      const formValue = { ...this.projectForm.value };
      delete formValue.id; // Remove the disabled id field

      if (formValue.startDate) {
        formValue.startDate = new Date(formValue.startDate).toISOString().split('T')[0];
      }
      if (formValue.endDate) {
        formValue.endDate = new Date(formValue.endDate).toISOString().split('T')[0];
      }
      this.dialogRef.close(formValue);
    }
  }
}
