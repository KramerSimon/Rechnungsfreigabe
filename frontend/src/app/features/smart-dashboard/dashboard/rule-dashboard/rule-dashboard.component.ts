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
import { RouterModule } from '@angular/router';
import { RuleDialogComponent } from './rule-dialog/rule-dialog.component';
import { ApprovalRule, ApprovalRuleActionDto, ApprovalRuleConditionDto, ApprovalRuleStageDto, CreateApprovalRuleDto, UpdateApprovalRuleDto } from '../../../../core/models/approval.model';
import { RuleCondition, RuleAction, RuleDialogData } from '../../../../core/models';
import { ProjectService } from '../../../../core/services/project.service';
import { ApprovalService } from '../../../../core/services/approval.service';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../../../core/services/user.service';
import { User } from '../../../../core/models/user.models';

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
    private userService: UserService
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
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '95vw',
      maxWidth: '1100px',
      data: { mode: 'create' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const dto = this.buildCreateRuleDto(result);

        this.approvalService.createApprovalRule(dto).subscribe({
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

  onEditRule(rule: any) {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '95vw',
      maxWidth: '1100px',
      data: { rule: { ...rule }, mode: 'edit' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const dto = this.buildUpdateRuleDto(result);

        this.approvalService.updateApprovalRule(rule.id, dto).subscribe({
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

  getFieldLabel(fieldValue: string): string {
    return this.availableFields.find(f => f.value === fieldValue)?.label || fieldValue;
  }

  getOperatorLabel(operatorValue: string): string {
    return this.availableOperators.find(o => o.value === operatorValue)?.label || operatorValue;
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
      role: s.role ?? null,
      userId: s.userId ?? null
    }));
  }
}
