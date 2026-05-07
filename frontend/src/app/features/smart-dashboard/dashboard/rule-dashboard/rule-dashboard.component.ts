import { CostCenterService } from '../../../../core/services/cost-center.service';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { ApprovalRule, ApprovalRuleActionDto, ApprovalRuleConditionDto, ApprovalRuleStageDto, CreateApprovalRuleDto, UpdateApprovalRuleDto } from '../../../../core/models/approval.model';
import { RuleCondition, RuleAction, RuleDialogData } from '../../../../core/models';
import { ProjectService } from '../../../../core/services/project.service';
import { ApprovalService } from '../../../../core/services/approval.service';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../../../core/services/user.service';
import { RoleDto, User } from '../../../../core/models/user.models';
import { RoleService } from '../../../../core/services/role.service';
import { CreateApprovalRuleDialogComponent } from '../../../master-data/tabs/approval-rules-tab/dialogs/create-approval-rule-dialog.component';
import { EditApprovalRuleDialogComponent } from '../../../master-data/tabs/approval-rules-tab/dialogs/edit-approval-rule-dialog/edit-approval-rule-dialog.component';
import { LanguageService } from '../../../../core/services/language.service';

export type { ApprovalRule, RuleCondition, RuleAction };

@Component({
  selector: 'app-rule-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDialogModule,
    MatExpansionModule,
    MatTabsModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatTooltipModule,
    RouterModule
  ],
  templateUrl: './rule-dashboard.component.html',
  styleUrls: ['./rule-dashboard.component.scss']
})
export class RuleDashboardComponent implements OnInit {
  activeTab = 0;

  // Freigabe-Regeln
  approvalRules: any[] = [];
  loading = false;

  // Kostenstellen
  costCenters: CostCenter[] = [];

  // Projekte
  projects: Project[] = [];
  // Benutzer für Zuweisungen
  users: User[] = [];
  roles: RoleDto[] = [];

  // Formular für neue Regel
  newRule: Partial<ApprovalRule> = {
    name: '',
    description: '',
    isActive: true,
    ruleType: 'manual',
    conditions: [],
    actions: [],
    priority: 10
  };

  // Neue Kostenstelle
  newCostCenter: Partial<CostCenter> = {
    id: '',
    name: '',
    description: '',
    managerId: ''
  };

  // Neues Projekt
  newProject: Partial<Project> = {
    id: '',
    name: '',
    costCenter: '',
    budget: 0,
    status: 'Geplant'
  };

  // Verfügbare Felder für Regeln
  availableFields = [
    { value: 'amount', label: 'Betrag' },
    { value: 'supplier', label: 'Lieferant' },
    { value: 'costCenter', label: 'Kostenstelle' },
    { value: 'project', label: 'Projekt' },
    { value: 'invoiceDate', label: 'Rechnungsdatum' },
    { value: 'dueDate', label: 'Fälligkeitsdatum' }
  ];

  // Verfügbare Operatoren
  availableOperators = [
    { value: '=', label: 'gleich' },
    { value: '!=', label: 'ungleich' },
    { value: '>', label: 'größer als' },
    { value: '<', label: 'kleiner als' },
    { value: '>=', label: 'größer oder gleich' },
    { value: '<=', label: 'kleiner oder gleich' },
    { value: 'contains', label: 'enthält' }
  ];

  // Verfügbare Aktionen
  availableActions = [
    { value: 'auto_approve', label: 'Automatisch freigeben' },
    { value: 'require_approval', label: 'Mehrstufige Freigabe' },
    { value: 'set_status', label: 'Status setzen' },
    { value: 'assign_to', label: 'Zuweisen an' }
  ];

  constructor(
    private dialog: MatDialog,
    private costCenterService: CostCenterService,
    private projectService: ProjectService,
    private approvalService: ApprovalService,
    private snackBar: MatSnackBar,
    private userService: UserService,
    private roleService: RoleService,
    private languageService: LanguageService
  ) {}

  ngOnInit() {
    this.loadApprovalRules();
    this.costCenterService.getCostCenters().subscribe((centers) => {
      this.costCenters = centers;
    });
    this.projectService.getProjects().subscribe((projects) => {
      this.projects = projects;
    });
    this.userService.getUsers().subscribe(users => {
      this.users = users || [];
    });
    this.roleService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles || [];
      },
      error: () => {
        this.roles = [];
      }
    });
  }

  private toBackendRuleType(rt: any): number {
    // Backend expects enum numeric: Automatic=0, Manual=1
    if (rt === 0 || rt === 1) return rt;
    const v = String(rt).toLowerCase();
    return v === 'automatic' ? 0 : 1;
  }

  loadApprovalRules() {
    this.loading = true;
    this.approvalService.getApprovalRules().subscribe({
      next: (rules) => {
        this.approvalRules = rules.map(rule => ({
          ...rule,
          conditions: this.parseConditions(rule.conditions),
          actions: this.parseActions(rule.actions)
        }));
        this.loading = false;
      },
      error: (error) => {
        console.error('Fehler beim Laden der Regeln:', error);
        this.snackBar.open(this.t('dashboard.rules.error.load'), this.t('common.close'), { duration: 3000 });
        this.loading = false;
      }
    });
  }

  private parseConditions(conditions: ApprovalRuleConditionDto[] | string | undefined): RuleCondition[] {
    if (!conditions) return [];
    if (typeof conditions === 'string') {
      try {
        return JSON.parse(conditions) as RuleCondition[];
      } catch {
        return [];
      }
    }
    return conditions.map(c => ({
      field: c.field,
      operator: c.operator,
      value: c.value,
      logicalOperator: c.logicalOperator
    }));
  }

  private parseActions(actions: ApprovalRuleActionDto[] | string | undefined): RuleAction[] {
    if (!actions) return [];
    if (typeof actions === 'string') {
      try {
        return JSON.parse(actions) as RuleAction[];
      } catch {
        return [];
      }
    }
    return actions.map(a => ({
      type: a.actionType as any,
      value: a.actionValue ?? '',
      description: a.description ?? '',
      stages: this.parseStages(a.stages)
    }));
  }

  private parseStages(stages?: ApprovalRuleStageDto[] | null): any[] | undefined {
    if (!stages || stages.length === 0) return undefined;
    return stages.map(s => ({
      stepNumber: s.stepNumber,
      approvalLevel: s.approvalLevel,
      role: s.role ?? undefined,
      userId: s.userId ?? undefined
    }));
  }

  // Regel-Management
  onCreateRule() {
    const dialogRef = this.dialog.open(CreateApprovalRuleDialogComponent, {
      width: '95vw',
      maxWidth: '1100px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const createDto = this.buildCreateRuleDto(result);

        this.approvalService.createApprovalRule(createDto).subscribe({
          next: () => {
            this.snackBar.open(this.t('dashboard.rules.success.created'), this.t('common.close'), { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error) => {
            console.error('Fehler beim Erstellen der Regel:', error);
            let errorMessage = this.t('dashboard.rules.error.create');
            if (error.status === 401) {
              errorMessage = this.t('dashboard.rules.error.unauthorized');
            } else if (error.status === 400 && error.error?.message) {
              errorMessage = error.error.message;
            }
            this.snackBar.open(errorMessage, this.t('common.close'), { duration: 5000 });
          }
        });
      }
    });
  }

  onEditRule(rule: ApprovalRule) {
    try {
      const dialogRule = this.mapToDialogRule(rule);
      const dialogRef = this.dialog.open(EditApprovalRuleDialogComponent, {
        width: '95vw',
        maxWidth: '1100px',
        data: dialogRule
      });

      dialogRef.afterClosed().subscribe(result => {
        if (result) {
          const updateDto = this.buildUpdateRuleDto(result);
          this.approvalService.updateApprovalRule(rule.id, updateDto).subscribe({
            next: () => {
              this.snackBar.open(this.t('dashboard.rules.success.updated'), this.t('common.close'), { duration: 3000 });
              this.loadApprovalRules();
            },
            error: (error) => {
              console.error('Fehler beim Aktualisieren der Regel:', error);
              this.snackBar.open(this.t('dashboard.rules.error.update'), this.t('common.close'), { duration: 3000 });
            }
          });
        }
      });
    } catch (error) {
      console.error('Fehler beim Offnen des Bearbeiten-Dialogs:', error);
      this.snackBar.open(this.t('dashboard.rules.error.update'), this.t('common.close'), { duration: 3000 });
    }
  }

  onEditRuleClick(event: MouseEvent, rule: ApprovalRule): void {
    event.preventDefault();
    event.stopPropagation();
    (event as any).stopImmediatePropagation?.();
    setTimeout(() => this.onEditRule(rule), 0);
  }

  private mapToDialogRule(rule: ApprovalRule): any {
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

  private normalizeRuleType(value: string): string {
    const lower = String(value ?? '').toLowerCase();
    return lower === 'automatic' || lower === 'manual' ? lower : 'manual';
  }

  onDeleteRule(ruleId: number) {
    if (confirm(this.t('dashboard.rules.confirm.delete'))) {
      this.approvalService.deleteApprovalRule(ruleId).subscribe({
        next: () => {
          this.snackBar.open(this.t('dashboard.rules.success.deleted'), this.t('common.close'), { duration: 3000 });
          this.loadApprovalRules();
        },
        error: (error) => {
          console.error('Fehler beim Löschen der Regel:', error);
          this.snackBar.open(this.t('dashboard.rules.error.delete'), this.t('common.close'), { duration: 3000 });
        }
      });
    }
  }

  onToggleRule(rule: any) {
    const newIsActive = !rule.isActive;
    const dto = this.buildUpdateRuleDto({ ...rule, isActive: newIsActive });

    this.approvalService.updateApprovalRule(rule.id, dto).subscribe({
      next: () => {
        rule.isActive = newIsActive;
        this.snackBar.open(
          rule.isActive ? this.t('dashboard.rules.success.activated') : this.t('dashboard.rules.success.deactivated'),
          this.t('common.close'),
          { duration: 2000 }
        );
      },
      error: (error) => {
        console.error('Fehler beim Umschalten der Regel:', error);
        this.snackBar.open(this.t('dashboard.rules.error.toggle'), this.t('common.close'), { duration: 3000 });
      }
    });
  }

  createApprovalRule(): void {
    this.onCreateRule();
  }

  editApprovalRule(rule: ApprovalRule): void {
    this.onEditRule(rule);
  }

  deleteApprovalRule(rule: ApprovalRule): void {
    this.onDeleteRule(rule.id);
  }

  toggleRuleActive(rule: ApprovalRule, event: MatSlideToggleChange): void {
    const dto = this.buildUpdateRuleDto({ ...rule, isActive: event.checked });

    this.approvalService.updateApprovalRule(rule.id, dto).subscribe({
      next: () => {
        rule.isActive = event.checked;
        this.snackBar.open(
          this.tp('dashboard.rules.success.toggled', { state: event.checked ? this.t('dashboard.rules.active') : this.t('dashboard.rules.inactive') }),
          this.t('common.close'),
          { duration: 2000 }
        );
      },
      error: (error) => {
        console.error('Fehler beim Aktualisieren der Regel:', error);
        event.source.checked = !event.checked;
        this.snackBar.open(this.t('dashboard.rules.error.update'), this.t('common.close'), { duration: 3000 });
      }
    });
  }

  // Hilfsfunktionen
  getRuleConditionText(conditions: RuleCondition[]): string {
    return conditions.map(condition => {
      const field = this.availableFields.find(f => f.value === condition.field)?.label || condition.field;
      const operator = this.availableOperators.find(o => o.value === condition.operator)?.label || condition.operator;
      return `${condition.logicalOperator ? condition.logicalOperator + ' ' : ''}${field} ${operator} ${condition.value}`;
    }).join(' ');
  }

  getRuleActionText(actions: RuleAction[]): string {
    return actions.map(action => {
      if (action.type === 'assign_to') {
        const user = this.users.find(u => String(u.id) === String(action.value));
        const name = user ? `${user.firstName} ${user.lastName}` : action.value;
        return this.tp('dashboard.rules.action.assignTo', { name });
      }
      if (action.type === 'require_approval') {
        const count = action.stages?.length || 0;
        return this.tp('dashboard.rules.action.multiStageWithCount', { count });
      }
      return action.description;
    }).join(', ');
  }

  getConditionFieldLabel(field: string): string {
    const fieldLabels: Record<string, string> = {
      amount: 'dashboard.rules.field.amount',
      supplier: 'dashboard.rules.field.supplier',
      costCenter: 'dashboard.rules.field.costCenter',
      project: 'dashboard.rules.field.project',
      invoiceDate: 'dashboard.rules.field.invoiceDate',
      dueDate: 'dashboard.rules.field.dueDate'
    };
    return fieldLabels[field] ? this.t(fieldLabels[field]) : field;
  }

  getOperatorLabel(operator: string): string {
    const operatorLabels: Record<string, string> = {
      equals: 'dashboard.rules.operator.equals',
      notEquals: 'dashboard.rules.operator.notEquals',
      greaterThan: 'dashboard.rules.operator.greaterThan',
      lessThan: 'dashboard.rules.operator.lessThan',
      greaterOrEqual: 'dashboard.rules.operator.greaterOrEqual',
      lessOrEqual: 'dashboard.rules.operator.lessOrEqual',
      contains: 'dashboard.rules.operator.contains',
      notContains: 'dashboard.rules.operator.notContains'
    };
    return operatorLabels[operator] ? this.t(operatorLabels[operator]) : operator;
  }

  getActionDescription(action: any): string {
    const actionType = String(action?.type ?? action?.actionType ?? '').toLowerCase();
    const actionValue = action?.value ?? action?.actionValue ?? '';

    switch (actionType) {
      case 'require_approval':
        return this.t('dashboard.rules.action.multiStage');
      case 'set_status':
        return this.tp('dashboard.rules.action.setStatus', { value: actionValue });
      case 'assign_to': {
        const user = this.users.find(u => String(u.id) === String(actionValue));
        const name = user ? `${user.firstName} ${user.lastName}` : actionValue;
        return this.tp('dashboard.rules.action.assignToWithColon', { name });
      }
      case 'notify':
        return this.t('dashboard.rules.action.notify');
      default:
        return action?.description || actionType || this.t('dashboard.rules.action.default');
    }
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
    return this.t('dashboard.rules.roleNotDefined');
  }

  private getRoleName(role: any): string {
    if (!role) return this.t('dashboard.rules.roleNotDefined');
    if (typeof role === 'object' && role.name) {
      return role.name;
    }
    if (typeof role === 'string') {
      return role;
    }
    return this.t('dashboard.rules.roleNotDefined');
  }

  getRuleTypeLabel(ruleType: any): string {
    if (ruleType === 0) {
      return this.t('dashboard.rules.type.automatic');
    }
    const normalized = String(ruleType ?? '').toLowerCase();
    return normalized === 'automatic' ? this.t('dashboard.rules.type.automatic') : this.t('dashboard.rules.type.manual');
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }

  tp(key: string, params: Record<string, string | number>): string {
    let translated = this.t(key);
    for (const [name, value] of Object.entries(params)) {
      translated = translated.replace(`{${name}}`, String(value));
    }
    return translated;
  }

  getCostCenterName(costCenterId: string): string {
    return this.costCenters.find(c => c.id === costCenterId)?.name || costCenterId;
  }

  private buildCreateRuleDto(rule: any): CreateApprovalRuleDto {
    return {
      name: rule.name,
      description: rule.description,
      ruleType: this.normalizeRuleTypeForApi(rule.ruleType),
      priority: rule.priority,
      conditions: this.mapConditions(rule.conditions),
      actions: this.mapActions(rule.actions),
      supplierId: rule.supplierId ?? null,
      costCenterId: rule.costCenterId ?? null,
      projectId: rule.projectId ?? null
    };
  }

  private buildUpdateRuleDto(rule: any): UpdateApprovalRuleDto {
    return {
      name: rule.name,
      description: rule.description,
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

  private normalizeRuleTypeForApi(value: any): 'Automatic' | 'Manual' {
    return String(value || '').toLowerCase() === 'automatic' ? 'Automatic' : 'Manual';
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

  private mapStages(stages: any): ApprovalRuleStageDto[] | undefined {
    if (!Array.isArray(stages) || stages.length === 0) {
      return undefined;
    }
    return stages.map((s: any) => ({
      stepNumber: Number(s.stepNumber ?? 1),
      approvalLevel: Number(s.approvalLevel ?? 1),
      roleId: s.roleId ?? (s.role?.id ?? null),
      userId: s.userId ?? null
    }));
  }

  private mapConditionsFromApi(conditions: any): any[] {
    if (!Array.isArray(conditions)) {
      return [];
    }

    return conditions.map((condition: any) => ({
      field: condition?.field ?? '',
      operator: condition?.operator ?? '=',
      value: condition?.value ?? '',
      logicalOperator: condition?.logicalOperator
    }));
  }

  private mapActionsFromApi(actions: any): any[] {
    if (!Array.isArray(actions)) {
      return [];
    }

    return actions.map((action: any) => ({
      // Accept both API DTO shape and UI shape so edit works for all loaded rules.
      type: action?.type ?? action?.actionType ?? 'require_approval',
      value: action?.value ?? action?.actionValue ?? '',
      description: action?.description ?? '',
      stages: this.mapStagesFromApi(action?.stages)
    }));
  }

  private mapStagesFromApi(stages?: any[]): any[] | undefined {
    if (!Array.isArray(stages) || stages.length === 0) {
      return undefined;
    }
    return stages.map(s => ({
      stepNumber: s.stepNumber,
      approvalLevel: s.approvalLevel,
      roleId: s.roleId ?? (s.role?.id) ?? undefined,
      role: typeof s.role === 'object' && s.role !== null ? s.role.name : (s.role ?? undefined),
      userId: s.userId ?? undefined
    }));
  }
}
