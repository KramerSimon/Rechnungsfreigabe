import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { ApprovalService } from '../../../../core/services/approval.service';
import { ApprovalRule, ApprovalRuleActionDto, ApprovalRuleConditionDto, ApprovalRuleStageDto, CreateApprovalRuleDto, UpdateApprovalRuleDto } from '../../../../core/models/approval.model';
import { RoleService } from '../../../../core/services/role.service';
import { RoleDto } from '../../../../core/models/user.models';
import { CreateApprovalRuleDialogComponent } from './dialogs/create-approval-rule-dialog.component';
import { EditApprovalRuleDialogComponent } from './dialogs/edit-approval-rule-dialog/edit-approval-rule-dialog.component';
import { LanguageService } from '../../../../core/services/language.service';

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
    MatTooltipModule,
    MatExpansionModule,
    MatSlideToggleModule
  ],
  templateUrl: './approval-rules-tab.component.html',
  styleUrls: ['./approval-rules-tab.component.scss']
})
export class ApprovalRulesTabComponent implements OnInit {
  displayedColumns: string[] = ['id', 'name', 'description', 'ruleType', 'priority', 'isActive', 'actions'];
  approvalRules: ApprovalRule[] = [];
  isLoading = false;
  roles: RoleDto[] = [];

  constructor(
    private approvalService: ApprovalService,
    private roleService: RoleService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadApprovalRules();
    this.loadRoles();
  }

  private loadRoles(): void {
    this.roleService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles || [];
      },
      error: (error: any) => {
        console.error('Error loading roles:', error);
        this.roles = [];
      }
    });
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
        this.snackBar.open(this.t('md.rules.error.load'), this.t('common.close'), { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  createApprovalRule(): void {
    const dialogRef = this.dialog.open(CreateApprovalRuleDialogComponent, {
      width: '95vw',
      maxWidth: '1100px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const createDto = this.mapToCreateDto(result);
        this.approvalService.createApprovalRule(createDto).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.rules.success.created'), this.t('common.close'), { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error: any) => {
            console.error('Error creating approval rule:', error);
            this.snackBar.open(this.t('md.rules.error.create'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  editApprovalRule(rule: ApprovalRule): void {
    this.isLoading = true;
    this.approvalService.getApprovalRuleById(rule.id).subscribe({
      next: (fullRule) => {
        this.isLoading = false;
        this.openEditDialog(fullRule ?? rule, rule.id);
      },
      error: (error: any) => {
        console.error('Error loading approval rule details:', error);
        this.isLoading = false;
        this.openEditDialog(rule, rule.id);
      }
    });
  }

  private openEditDialog(rule: ApprovalRule, ruleId: number): void {
    const dialogRule = this.mapToDialogRule(rule);
    const dialogRef = this.dialog.open(EditApprovalRuleDialogComponent, {
      width: '95vw',
      maxWidth: '1100px',
      data: dialogRule
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const updateDto = this.mapToUpdateDto(result);
        this.approvalService.updateApprovalRule(ruleId, updateDto).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.rules.success.updated'), this.t('common.close'), { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error: any) => {
            console.error('Error updating approval rule:', error);
            this.snackBar.open(this.t('md.rules.error.update'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  deleteApprovalRule(rule: ApprovalRule): void {
    if (confirm(this.t('md.rules.confirm.delete').replace('{name}', rule.name))) {
      this.approvalService.deleteApprovalRule(rule.id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.rules.success.deleted'), this.t('common.close'), { duration: 3000 });
          this.loadApprovalRules();
        },
        error: (error: any) => {
          console.error('Error deleting approval rule:', error);
          this.snackBar.open(this.t('md.rules.error.delete'), this.t('common.close'), { duration: 3000 });
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
      actionValue: a.value !== undefined && a.value !== null ? String(a.value) : null,
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
      roleId: s.roleId !== undefined && s.roleId !== null && s.roleId !== ''
        ? Number(s.roleId)
        : null,
      role: s.role ? String(s.role) : null,
      userId: s.userId !== undefined && s.userId !== null && s.userId !== ''
        ? Number(s.userId)
        : null
    }));
  }

  private mapStagesFromApi(stages?: ApprovalRuleStageDto[]): any[] | undefined {
    if (!Array.isArray(stages) || stages.length === 0) {
      return undefined;
    }
    return stages.map(s => ({
      stepNumber: s.stepNumber,
      approvalLevel: s.approvalLevel,
      roleId: s.roleId ?? ((s.role as any)?.id) ?? undefined,
      role: typeof s.role === 'object' && s.role !== null ? (s.role as any).name : (s.role ?? undefined),
      userId: s.userId ?? undefined
    }));
  }

  toggleRuleActive(rule: ApprovalRule, event: MatSlideToggleChange): void {
    const updateDto: UpdateApprovalRuleDto = {
      name: rule.name,
      description: rule.description || '',
      ruleType: this.normalizeRuleTypeForApi(rule.ruleType),
      priority: rule.priority,
      isActive: event.checked,
      conditions: this.mapConditionsFromApi(rule.conditions),
      actions: this.mapActionsFromApiForUpdate(rule.actions),
      supplierId: rule.supplierId ?? null,
      costCenterId: rule.costCenterId ?? null,
      projectId: rule.projectId ?? null
    };

    this.approvalService.updateApprovalRule(rule.id, updateDto).subscribe({
      next: () => {
        rule.isActive = event.checked;
        this.snackBar.open(this.t(event.checked ? 'md.rules.success.activated' : 'md.rules.success.deactivated'), this.t('common.close'), { duration: 2000 });
      },
      error: (error: any) => {
        console.error('Error updating rule status:', error);
        event.source.checked = !event.checked;
        this.snackBar.open(this.t('md.rules.error.update'), this.t('common.close'), { duration: 3000 });
      }
    });
  }

  private mapActionsFromApiForUpdate(actions?: ApprovalRuleActionDto[]): ApprovalRuleActionDto[] {
    if (!Array.isArray(actions)) {
      return [];
    }
    return actions.map(a => ({
      actionType: a.actionType,
      actionValue: a.actionValue ?? null,
      description: a.description ?? null,
      stages: a.stages
    }));
  }

  getConditionFieldLabel(field: string): string {
    const fieldLabels: Record<string, string> = {
      amount: 'md.rules.field.amount',
      supplier: 'md.rules.field.supplier',
      costCenter: 'md.rules.field.costCenter',
      project: 'md.rules.field.project',
      invoiceDate: 'md.rules.field.invoiceDate',
      dueDate: 'md.rules.field.dueDate'
    };
    return fieldLabels[field] ? this.t(fieldLabels[field]) : field;
  }

  getOperatorLabel(operator: string): string {
    const operatorLabels: Record<string, string> = {
      equals: 'md.rules.operator.equals',
      notEquals: 'md.rules.operator.notEquals',
      greaterThan: 'md.rules.operator.greaterThan',
      lessThan: 'md.rules.operator.lessThan',
      greaterOrEqual: 'md.rules.operator.greaterOrEqual',
      lessOrEqual: 'md.rules.operator.lessOrEqual',
      contains: 'md.rules.operator.contains',
      notContains: 'md.rules.operator.notContains'
    };
    return operatorLabels[operator] ? this.t(operatorLabels[operator]) : operator;
  }

  getActionDescription(action: ApprovalRuleActionDto): string {
    switch (action.actionType?.toLowerCase()) {
      case 'require_approval':
        return this.t('md.rules.action.multiStage');
      case 'set_status':
        return `${this.t('md.rules.action.setStatus')}: ${action.actionValue}`;
      case 'assign_to':
        return `${this.t('md.rules.action.assignTo')}: ${action.actionValue}`;
      case 'notify':
        return this.t('md.rules.action.notify');
      default:
        return action.description || action.actionType || this.t('md.rules.action.default');
    }
  }

  getRoleName(role: any): string {
    if (!role) return this.t('md.rules.roleNotDefined');
    if (typeof role === 'object' && role.name) {
      return role.name;
    }
    if (typeof role === 'string') {
      return role;
    }
    return this.t('md.rules.roleNotDefined');
  }

  getRoleNameFromStage(stage: { role?: any; roleId?: number | null }): string {
    if (stage?.role) {
      return this.getRoleName(stage.role);
    }
    if (stage?.roleId !== undefined && stage?.roleId !== null) {
      const match = this.roles.find(r => r.id === Number(stage.roleId));
      if (match?.name) {
        return match.name;
      }
    }
    return this.t('md.rules.roleNotDefined');
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
