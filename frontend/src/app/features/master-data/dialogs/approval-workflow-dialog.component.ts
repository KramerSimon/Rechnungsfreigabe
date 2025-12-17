import { InvoiceService } from './../../../core/services/invoice.service';
import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApprovalWorkflow, CreateApprovalWorkflowDto, UpdateApprovalWorkflowDto } from '../../../core/models/approval.model';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.models';
import { Invoice } from '../../../core';

export interface ApprovalWorkflowDialogData {
  mode: 'create' | 'edit';
  workflow?: ApprovalWorkflow;
}

@Component({
  selector: 'app-approval-workflow-dialog',
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
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>
        {{ data.mode === 'create' ? 'Neuen Genehmigungsworkflow anlegen' : 'Genehmigungsworkflow bearbeiten' }}
      </h2>

      <form [formGroup]="form" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Rechnungs-ID *</mat-label>
            <mat-select formControlName="invoiceId" required>
              <mat-option *ngFor="let i of invoices" [value]="i.id">
                {{ i.invoiceNumber }}
              </mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Schritt *</mat-label>
            <input matInput type="number" formControlName="stepNumber" required>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Genehmiger *</mat-label>
            <mat-select formControlName="approverId" required>
              <mat-option *ngFor="let u of users" [value]="u.id">
                {{u.firstName}} {{u.lastName}} ({{u.username}})
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Genehmigungsstufe *</mat-label>
            <input matInput type="number" formControlName="approvalLevel" required>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              <mat-option value="Pending">Ausstehend</mat-option>
              <mat-option value="Approved">Genehmigt</mat-option>
              <mat-option value="Rejected">Abgelehnt</mat-option>
              <mat-option value="Skipped">Übersprungen</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Kommentare</mat-label>
            <textarea matInput formControlName="comments" rows="3"></textarea>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary" [disabled]="!form.valid" (click)="onSave()">
          {{ data.mode === 'create' ? 'Erstellen' : 'Speichern' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container { padding: 20px; min-width: 480px; }
    .form-row { display: flex; gap: 16px; margin-bottom: 16px; align-items: center; }
    .full-width { width: 100%; }
    mat-form-field { flex: 1; }
  `]
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

  ngOnInit(): void {
    this.invoiceService.getInvoices().subscribe(result => this.invoices = result.items);
    this.userService.getUsers().subscribe(users => this.users = users);
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
