import { InvoiceService } from '../../../../core/services/invoice.service';
import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApprovalWorkflow, CreateApprovalWorkflowDto, UpdateApprovalWorkflowDto } from '../../../../core/models/approval.model';
import { UserService } from '../../../../core/services/user.service';
import { User } from '../../../../core/models/user.models';
import { Invoice } from '../../../../core/models/invoice.models';

export interface ApprovalWorkflowDialogData {
  mode: 'create' | 'edit';
  workflow?: ApprovalWorkflow;
}

@Component({
  selector: 'app-approval-workflow-dialog',
  templateUrl: './approval-workflow-dialog.component.html',
  styleUrls: ['./approval-workflow-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
  ]
})
export class ApprovalWorkflowDialogComponent implements OnInit {
  form: FormGroup;
  users: User[] = [];
  invoices: Invoice[] = [];

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private userService: UserService,
    public dialogRef: MatDialogRef<ApprovalWorkflowDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ApprovalWorkflowDialogData
  ) {
    this.form = this.fb.group({
      invoiceId: [data.workflow?.invoiceId ?? null, [Validators.required]],
      stepNumber: [data.workflow?.stepNumber ?? 1, [Validators.required, Validators.min(1)]],
      approverId: [data.workflow?.approverId ?? null, [Validators.required]],
      approvalLevel: [data.workflow?.approvalLevel ?? 1, [Validators.required, Validators.min(1)]],
      status: [data.workflow?.status ?? 'Pending'],
      comments: [data.workflow?.comments ?? '']
    });
  }

  getStatusMeta(status?: string | null): { label: string; color: string } {
    switch (status) {
      case 'Pending':
        return { label: 'Ausstehend', color: '#FFA500' };
      case 'Waiting':
        return { label: 'Wartend', color: '#2196F3' };
      case 'Approved':
        return { label: 'Genehmigt', color: '#4CAF50' };
      case 'Rejected':
        return { label: 'Abgelehnt', color: '#F44336' };
      case 'Skipped':
        return { label: 'Übersprungen', color: '#9E9E9E' };
      default:
        return { label: '—', color: '#9E9E9E' };
    }
  }

  ngOnInit(): void {
    this.invoiceService.getInvoices().subscribe((result: any) => this.invoices = result.items);
    this.userService.getUsers().subscribe((users: User[]) => this.users = users);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (!this.form.valid) return;

    const value = this.form.value;

    if (this.data.mode === 'create') {
      const payload: CreateApprovalWorkflowDto = {
        invoiceId: Number(value.invoiceId),
        stepNumber: Number(value.stepNumber),
        approverId: Number(value.approverId),
        approvalLevel: Number(value.approvalLevel),
        status: value.status,
        comments: value.comments || undefined,
      };
      this.dialogRef.close(payload);
    } else {
      const payload: UpdateApprovalWorkflowDto = {
        invoiceId: Number(value.invoiceId),
        stepNumber: Number(value.stepNumber),
        approverId: Number(value.approverId),
        approvalLevel: Number(value.approvalLevel),
        status: value.status,
        comments: value.comments ?? undefined,
      };
      this.dialogRef.close(payload);
    }
  }
}
