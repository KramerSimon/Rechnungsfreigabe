import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CostCenter } from '../../../../../../core/models/cost-center.model';
import { User } from '../../../../../../core/models/user.models';

@Component({
  selector: 'app-edit-cost-center-dialog',
  templateUrl: './edit-cost-center-dialog.component.html',
  styleUrls: ['./edit-cost-center-dialog.component.scss'],
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
