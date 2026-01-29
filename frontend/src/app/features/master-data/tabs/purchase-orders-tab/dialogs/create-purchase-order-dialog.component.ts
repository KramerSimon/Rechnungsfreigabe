import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CostCenter } from '../../../../../core/models/cost-center.model';
import { Project } from '../../../../../core/models/project.model';
import { CreatePurchaseOrderRequest } from '../../../../../core/models/purchaseOrder.model';

interface CreatePurchaseOrderDialogData {
  costCenters: CostCenter[];
  projects: Project[];
}

@Component({
  selector: 'app-create-purchase-order-dialog',
  templateUrl: './create-purchase-order-dialog.component.html',
  styleUrls: ['./create-purchase-order-dialog.component.scss'],
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
