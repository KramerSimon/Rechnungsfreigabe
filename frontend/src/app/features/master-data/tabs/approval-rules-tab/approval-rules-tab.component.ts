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
import { ApprovalRule, CreateApprovalRuleDto } from '../../../../core/models/approval.model';
import { RuleDialogComponent } from '../../../smart-dashboard/dashboard/rule-dashboard/rule-dialog/rule-dialog.component';

// AdminRule interface for dialog compatibility
interface AdminRule {
  id?: number;
  name: string;
  description?: string;
  ruleType: string;
  priority: number;
  isActive: boolean;
  conditions: any;
  actions: any;
}

interface RuleDialogData {
  mode: 'create' | 'edit';
  rule?: AdminRule;
}

@Component({
  selector: 'app-approval-rules-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './approval-rules-tab.component.html',
  styleUrls: ['./approval-rules-tab.component.scss']
})
export class ApprovalRulesTabComponent implements OnInit {
  displayedColumns: string[] = ['id', 'name', 'description', 'ruleType', 'priority', 'isActive', 'actions'];
  approvalRules: ApprovalRule[] = [];
  isLoading = false;

  constructor(
    private approvalService: ApprovalService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadApprovalRules();
  }

  loadApprovalRules(): void {
    this.isLoading = true;
    this.approvalService.getApprovalRules().subscribe({
      next: (rules) => {
        this.approvalRules = rules;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading approval rules:', error);
        this.snackBar.open('Failed to load approval rules', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  createApprovalRule(): void {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { mode: 'create' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const createDto = this.mapFromDialogRule(result);
        this.approvalService.createApprovalRule(createDto).subscribe({
          next: () => {
            this.snackBar.open('Approval rule created successfully', 'Close', { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error: any) => {
            console.error('Error creating approval rule:', error);
            this.snackBar.open('Failed to create approval rule', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  editApprovalRule(rule: ApprovalRule): void {
    const dialogRule = this.mapToDialogRule(rule);
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { mode: 'edit', rule: dialogRule } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const updateDto = this.mapFromDialogRule(result);
        this.approvalService.updateApprovalRule(rule.id, updateDto).subscribe({
          next: () => {
            this.snackBar.open('Approval rule updated successfully', 'Close', { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error: any) => {
            console.error('Error updating approval rule:', error);
            this.snackBar.open('Failed to update approval rule', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  deleteApprovalRule(rule: ApprovalRule): void {
    if (confirm(`Are you sure you want to delete the approval rule "${rule.name}"?`)) {
      this.approvalService.deleteApprovalRule(rule.id).subscribe({
        next: () => {
          this.snackBar.open('Approval rule deleted successfully', 'Close', { duration: 3000 });
          this.loadApprovalRules();
        },
        error: (error: any) => {
          console.error('Error deleting approval rule:', error);
          this.snackBar.open('Failed to delete approval rule', 'Close', { duration: 3000 });
        }
      });
    }
  }

  // Map ApprovalRule to AdminRule for dialog
  private mapToDialogRule(rule: ApprovalRule): AdminRule {
    return {
      id: rule.id,
      name: rule.name,
      description: rule.description || '',
      ruleType: rule.ruleType,
      priority: rule.priority,
      isActive: rule.isActive,
      conditions: typeof rule.conditions === 'string' ? JSON.parse(rule.conditions) : rule.conditions,
      actions: typeof rule.actions === 'string' ? JSON.parse(rule.actions) : rule.actions
    };
  }

  // Map AdminRule to CreateApprovalRuleDto for API
  private mapFromDialogRule(rule: AdminRule): CreateApprovalRuleDto {
    return {
      name: rule.name,
      description: rule.description || '',
      ruleType: rule.ruleType,
      priority: rule.priority,
      isActive: rule.isActive,
      conditions: typeof rule.conditions === 'object' ? JSON.stringify(rule.conditions) : rule.conditions,
      actions: typeof rule.actions === 'object' ? JSON.stringify(rule.actions) : rule.actions
    };
  }
}
