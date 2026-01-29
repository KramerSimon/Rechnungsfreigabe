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
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';
import { User } from '../../../../core/models/user.models';


@Component({
  selector: 'app-edit-project-dialog',
  templateUrl: './edit-project-dialog.component.html',
  styleUrls: ['./edit-project-dialog.component.scss'],
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
export class EditProjectDialogComponent {
  projectForm: FormGroup;
  costCenters: CostCenter[] = [];
  projectManagers: User[] = [];

  constructor(
    private dialogRef: MatDialogRef<EditProjectDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { project: Project, costCenters: CostCenter[], projectManagers: User[] },
    private fb: FormBuilder
  ) {
    this.costCenters = data.costCenters;
    this.projectManagers = data.projectManagers || [];
    const project = data.project;

    this.projectForm = this.fb.group({
      id: [{ value: project.id || '', disabled: true }],
      name: [project.name || '', [Validators.required, Validators.maxLength(100)]],
      description: [project.description || ''],
      costCenterId: [project.costCenterId || '', [Validators.required]],
      budget: [project.budget || 0, [Validators.min(0)]],
      status: [project.status || 'Geplant'],
      startDate: [project.startDate ? new Date(project.startDate) : null],
      endDate: [project.endDate ? new Date(project.endDate) : null],
      projectManagerId: [project.projectManagerId || null]
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
      // Format dates properly and include all fields including disabled id
      const formValue = { ...this.projectForm.getRawValue() };

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
