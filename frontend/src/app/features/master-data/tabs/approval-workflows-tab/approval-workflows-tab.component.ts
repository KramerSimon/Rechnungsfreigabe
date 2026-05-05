import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApprovalService } from '../../../../core/services/approval.service';
import { ApprovalWorkflow, CreateApprovalWorkflowDto, UpdateApprovalWorkflowDto } from '../../../../core/models/approval.model';
import { ApprovalWorkflowDialogComponent, ApprovalWorkflowDialogData } from './dialogs/approval-workflow-dialog.component';
import { StatusDisplayPipe } from '../../../../core/pipes/status-display.pipe';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-approval-workflows-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    StatusDisplayPipe
  ],
  templateUrl: './approval-workflows-tab.component.html',
  styleUrls: ['./approval-workflows-tab.component.scss']
})
export class ApprovalWorkflowsTabComponent implements OnInit {
  displayedColumns: string[] = [
    'id',
    'invoiceId',
    'approverId',
    'approvalLevel',
    'status',
    'createdAt',
    'actions'
  ];
  approvalWorkflows: ApprovalWorkflow[] = [];
  isLoading = false;

  constructor(
    private approvalService: ApprovalService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadApprovalWorkflows();
  }

  loadApprovalWorkflows(): void {
    this.isLoading = true;
    this.approvalService.getApprovalWorkflows().subscribe({
      next: (workflows: ApprovalWorkflow[]) => {
        this.approvalWorkflows = workflows;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading approval workflows:', error);
        this.snackBar.open(this.t('md.workflows.error.load'), this.t('common.close'), { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  createApprovalWorkflow(): void {
    const dialogRef = this.dialog.open(ApprovalWorkflowDialogComponent, {
      width: '600px',
      data: { mode: 'create' } as ApprovalWorkflowDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.approvalService.createApprovalWorkflow(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.workflows.success.created'), this.t('common.close'), { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error: any) => {
            console.error('Error creating approval workflow:', error);
            this.snackBar.open(this.t('md.workflows.error.create'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  editApprovalWorkflow(workflow: ApprovalWorkflow): void {
    const dialogRef = this.dialog.open(ApprovalWorkflowDialogComponent, {
      width: '600px',
      data: { mode: 'edit', workflow: { ...workflow } } as ApprovalWorkflowDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.approvalService.updateApprovalWorkflow(workflow.id, result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.workflows.success.updated'), this.t('common.close'), { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error: any) => {
            console.error('Error updating approval workflow:', error);
            this.snackBar.open(this.t('md.workflows.error.update'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  deleteApprovalWorkflow(id: number): void {
    if (confirm(this.t('md.workflows.confirm.delete'))) {
      this.approvalService.deleteApprovalWorkflow(id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.workflows.success.deleted'), this.t('common.close'), { duration: 3000 });
          this.loadApprovalWorkflows();
        },
        error: (error: any) => {
          console.error('Error deleting approval workflow:', error);
          this.snackBar.open(this.t('md.workflows.error.delete'), this.t('common.close'), { duration: 3000 });
        }
      });
    }
  }

  formatDateTime(dateTime: any): string {
    if (!dateTime) return '-';
    const date = new Date(dateTime);
    return date.toLocaleString('de-DE', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  adjustBackgroundOpacity(color: string): string {
    // Return color as-is without opacity
    return color || 'transparent';
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
