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
    private roleService: RoleService
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
        this.snackBar.open('Fehler beim Laden der Freigaberegeln', 'Schließen', { duration: 3000 });
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
            this.snackBar.open('Regel erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadApprovalRules();
          },
          error: (error) => {
            console.error('Fehler beim Erstellen der Regel:', error);
            let errorMessage = 'Fehler beim Erstellen der Regel';
            if (error.status === 401) {
              errorMessage = 'Nicht berechtigt. Bitte melden Sie sich als Administrator an.';
            } else if (error.status === 400 && error.error?.message) {
              errorMessage = error.error.message;
            }
            this.snackBar.open(errorMessage, 'Schließen', { duration: 5000 });
          }
        });
      }
    });
  }

  onEditRule(rule: ApprovalRule) {
    this.loading = true;
    this.approvalService.getApprovalRuleById(rule.id).subscribe({
      next: (fullRule) => {
        this.loading = false;
        const dialogRule = this.mapToDialogRule(fullRule ?? rule);
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
                this.snackBar.open('Regel erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
                this.loadApprovalRules();
              },
              error: (error) => {
                console.error('Fehler beim Aktualisieren der Regel:', error);
                this.snackBar.open('Fehler beim Aktualisieren der Regel', 'Schließen', { duration: 3000 });
              }
            });
          }
        });
      },
      error: (error) => {
        console.error('Fehler beim Laden der Regeldetails:', error);
        this.loading = false;
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
                this.snackBar.open('Regel erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
                this.loadApprovalRules();
              },
              error: (error) => {
                console.error('Fehler beim Aktualisieren der Regel:', error);
                this.snackBar.open('Fehler beim Aktualisieren der Regel', 'Schließen', { duration: 3000 });
              }
            });
          }
        });
      }
    });
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
    const lower = (value || '').toLowerCase();
    return lower === 'automatic' || lower === 'manual' ? lower : 'manual';
  }

  onDeleteRule(ruleId: number) {
    if (confirm('Regel wirklich löschen?')) {
      this.approvalService.deleteApprovalRule(ruleId).subscribe({
        next: () => {
          this.snackBar.open('Regel erfolgreich gelöscht', 'Schließen', { duration: 3000 });
          this.loadApprovalRules();
        },
        error: (error) => {
          console.error('Fehler beim Löschen der Regel:', error);
          this.snackBar.open('Fehler beim Löschen der Regel', 'Schließen', { duration: 3000 });
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
          rule.isActive ? 'Regel aktiviert' : 'Regel deaktiviert',
          'Schließen',
          { duration: 2000 }
        );
      },
      error: (error) => {
        console.error('Fehler beim Umschalten der Regel:', error);
        this.snackBar.open('Fehler beim Umschalten der Regel', 'Schließen', { duration: 3000 });
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
        this.snackBar.open(`Regel ${event.checked ? 'aktiviert' : 'deaktiviert'}`, 'Schließen', { duration: 2000 });
      },
      error: (error) => {
        console.error('Fehler beim Aktualisieren der Regel:', error);
        event.source.checked = !event.checked;
        this.snackBar.open('Fehler beim Aktualisieren der Regel', 'Schließen', { duration: 3000 });
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
        return `Zuweisen an ${name}`;
      }
      if (action.type === 'require_approval') {
        const count = action.stages?.length || 0;
        return `Mehrstufige Freigabe (${count} Stufen)`;
      }
      return action.description;
    }).join(', ');
  }

  getConditionFieldLabel(field: string): string {
    const fieldLabels: { [key: string]: string } = {
      'amount': 'Betrag',
      'supplier': 'Lieferant',
      'costCenter': 'Kostenstelle',
      'project': 'Projekt',
      'invoiceDate': 'Rechnungsdatum',
      'dueDate': 'Fälligkeitsdatum'
    };
    return fieldLabels[field] || field;
  }

  getOperatorLabel(operator: string): string {
    const operatorLabels: { [key: string]: string } = {
      'equals': 'gleich',
      'notEquals': 'ungleich',
      'greaterThan': 'größer als',
      'lessThan': 'kleiner als',
      'greaterOrEqual': 'größer oder gleich',
      'lessOrEqual': 'kleiner oder gleich',
      'contains': 'enthält',
      'notContains': 'enthält nicht'
    };
    return operatorLabels[operator] || operator;
  }

  getActionDescription(action: any): string {
    const actionType = String(action?.type ?? action?.actionType ?? '').toLowerCase();
    const actionValue = action?.value ?? action?.actionValue ?? '';

    switch (actionType) {
      case 'require_approval':
        return 'Mehrstufige Freigabe';
      case 'set_status':
        return `Status setzen: ${actionValue}`;
      case 'assign_to': {
        const user = this.users.find(u => String(u.id) === String(actionValue));
        const name = user ? `${user.firstName} ${user.lastName}` : actionValue;
        return `Zuweisen an: ${name}`;
      }
      case 'notify':
        return 'Benachrichtigung senden';
      default:
        return action?.description || actionType || 'Aktion';
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
    return 'Rolle nicht definiert';
  }

  private getRoleName(role: any): string {
    if (!role) return 'Rolle nicht definiert';
    if (typeof role === 'object' && role.name) {
      return role.name;
    }
    if (typeof role === 'string') {
      return role;
    }
    return 'Rolle nicht definiert';
  }

  getRuleTypeLabel(ruleType: any): string {
    if (ruleType === 0) {
      return 'Automatische Freigabe';
    }
    const normalized = String(ruleType ?? '').toLowerCase();
    return normalized === 'automatic' ? 'Automatische Freigabe' : 'Manuelle Freigabe';
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
    if (!conditions) return [];
    return Array.isArray(conditions) ? conditions : [];
  }

  private mapActionsFromApi(actions: any): any[] {
    if (!actions) return [];
    if (!Array.isArray(actions)) return [];

    return actions.map((action: any) => ({
      type: action.actionType,
      value: action.actionValue ?? '',
      description: action.description ?? '',
      stages: this.mapStagesFromApi(action.stages)
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
