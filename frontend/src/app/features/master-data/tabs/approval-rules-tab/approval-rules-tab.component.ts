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
import { ApprovalRule, ApprovalRuleActionDto, ApprovalRuleConditionDto, ApprovalRuleStageDto, CreateApprovalRuleDto, UpdateApprovalRuleDto } from '../../../../core/models/approval.model';
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
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
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
      width: '95vw',
      maxWidth: '1100px',
      data: { mode: 'create' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const createDto = this.mapToCreateDto(result);
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
      width: '95vw',
      maxWidth: '1100px',
      data: { mode: 'edit', rule: dialogRule } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const updateDto = this.mapToUpdateDto(result);
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
      ruleType: this.normalizeRuleType(rule.ruleType),
      priority: rule.priority,
      isActive: rule.isActive,
      conditions: this.mapConditionsFromApi(rule.conditions),
      actions: this.mapActionsFromApi(rule.actions),
      supplierId: rule.supplierId ?? null,
      costCenterId: rule.costCenterId ?? null,
      projectId: rule.projectId ?? null
    };
  }

  private mapToCreateDto(rule: AdminRule): CreateApprovalRuleDto {
    return {
      name: rule.name,
      description: rule.description || '',
      ruleType: this.normalizeRuleTypeForApi(rule.ruleType),
      priority: rule.priority,
      conditions: this.mapConditions(rule.conditions),
      actions: this.mapActions(rule.actions),
      supplierId: rule.supplierId ?? null,
      costCenterId: rule.costCenterId ?? null,
      projectId: rule.projectId ?? null
    };
  }

  private mapToUpdateDto(rule: AdminRule): UpdateApprovalRuleDto {
    return {
      name: rule.name,
      description: rule.description || '',
      ruleType: this.normalizeRuleTypeForApi(rule.ruleType),
      priority: rule.priority,
      isActive: rule.isActive,
      conditions: this.mapConditions(rule.conditions),
      actions: this.mapActions(rule.actions),
      supplierId: rule.supplierId ?? null,
      costCenterId: rule.costCenterId ?? null,
      projectId: rule.projectId ?? null
    };
  }

  private normalizeRuleType(value: string): string {
    const lower = (value || '').toLowerCase();
    return lower === 'automatic' || lower === 'manual' ? lower : 'manual';
  }

  private normalizeRuleTypeForApi(value: string): 'Automatic' | 'Manual' {
    return (value || '').toLowerCase() === 'automatic' ? 'Automatic' : 'Manual';
  }

  private mapConditions(conditions: any): ApprovalRuleConditionDto[] {
    if (!Array.isArray(conditions)) {
      return [];
    }
    return conditions.map((c: any) => ({
      field: c.field,
      operator: c.operator,
      value: String(c.value ?? ''),
      logicalOperator: c.logicalOperator
    }));
  }

  private mapConditionsFromApi(conditions?: ApprovalRuleConditionDto[]): any[] {
    if (!Array.isArray(conditions)) {
      return [];
    }
    return conditions.map(c => ({
      field: c.field,
      operator: c.operator,
      value: c.value,
      logicalOperator: c.logicalOperator
    }));
  }

  private mapActions(actions: any): ApprovalRuleActionDto[] {
    if (!Array.isArray(actions)) {
      return [];
    }
    return actions.map((a: any) => ({
      actionType: a.type,
      actionValue: a.value ?? null,
      description: a.description ?? null,
      stages: this.mapStages(a.stages)
    }));
  }

  private mapActionsFromApi(actions?: ApprovalRuleActionDto[]): any[] {
    if (!Array.isArray(actions)) {
      return [];
    }
    return actions.map(a => ({
      type: a.actionType,
      value: a.actionValue ?? '',
      description: a.description ?? '',
      stages: this.mapStagesFromApi(a.stages)
    }));
  }

  private mapStages(stages: any): ApprovalRuleStageDto[] | undefined {
    if (!Array.isArray(stages) || stages.length === 0) {
      return undefined;
    }
    return stages.map((s: any) => ({
      stepNumber: Number(s.stepNumber ?? 1),
      approvalLevel: Number(s.approvalLevel ?? 1),
      role: s.role ?? null,
      userId: s.userId ?? null
    }));
  }

  private mapStagesFromApi(stages?: ApprovalRuleStageDto[]): any[] | undefined {
    if (!Array.isArray(stages) || stages.length === 0) {
      return undefined;
    }
    return stages.map(s => ({
      stepNumber: s.stepNumber,
      approvalLevel: s.approvalLevel,
      role: s.role ?? undefined,
      userId: s.userId ?? undefined
    }));
  }
}
