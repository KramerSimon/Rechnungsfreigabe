import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';
import { User } from '../../../../core/models/user.models';

@Component({
  selector: 'app-create-project-dialog',
  templateUrl: './create-project-dialog.component.html',
  styleUrls: ['./create-project-dialog.component.scss'],
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
  ]
})
export class CreateProjectDialogComponent {
  projectForm: FormGroup;
  costCenters: CostCenter[] = [];
  projectManagers: User[] = [];

  constructor(
    private dialogRef: MatDialogRef<CreateProjectDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { project: Project, costCenters: CostCenter[], projectManagers: User[] },
    private fb: FormBuilder
  ) {
    this.costCenters = data.costCenters;
    this.projectManagers = data.projectManagers || [];

    this.projectForm = this.fb.group({
      id: [data.project.id || '', [Validators.required, Validators.maxLength(20)]],
      name: [data.project.name || '', [Validators.required, Validators.maxLength(100)]],
      description: [data.project.description || ''],
      costCenterId: [data.project.costCenterId || '', [Validators.required]],
      budget: [data.project.budget || 0, [Validators.min(0)]],
      status: [data.project.status || 'Geplant'],
      startDate: [data.project.startDate || null],
      endDate: [data.project.endDate || null],
      projectManagerId: [data.project.projectManagerId || null]
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
      // Format dates properly
      const formValue = { ...this.projectForm.value };
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
