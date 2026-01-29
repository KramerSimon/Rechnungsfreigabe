import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApprovalService } from '../../../../core/services/approval.service';
import { ApprovalWorkflow, CreateApprovalWorkflowDto, UpdateApprovalWorkflowDto } from '../../../../core/models/approval.model';
import { ApprovalWorkflowDialogComponent, ApprovalWorkflowDialogData } from './dialogs/approval-workflow-dialog.component';
import { StatusDisplayPipe } from '../../../../core/pipes/status-display.pipe';

@Component({
  selector: 'app-approval-workflows-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
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
    private snackBar: MatSnackBar
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
        this.snackBar.open('Failed to load approval workflows', 'Close', { duration: 3000 });
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
            this.snackBar.open('Approval workflow created successfully', 'Close', { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error: any) => {
            console.error('Error creating approval workflow:', error);
            this.snackBar.open('Failed to create approval workflow', 'Close', { duration: 3000 });
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
            this.snackBar.open('Approval workflow updated successfully', 'Close', { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error: any) => {
            console.error('Error updating approval workflow:', error);
            this.snackBar.open('Failed to update approval workflow', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  deleteApprovalWorkflow(workflow: ApprovalWorkflow): void {
    if (confirm(`Are you sure you want to delete the approval workflow #${workflow.id}?`)) {
      this.approvalService.deleteApprovalWorkflow(workflow.id).subscribe({
        next: () => {
          this.snackBar.open('Approval workflow deleted successfully', 'Close', { duration: 3000 });
          this.loadApprovalWorkflows();
        },
        error: (error: any) => {
          console.error('Error deleting approval workflow:', error);
          this.snackBar.open('Failed to delete approval workflow', 'Close', { duration: 3000 });
        }
      });
    }
  }
}
