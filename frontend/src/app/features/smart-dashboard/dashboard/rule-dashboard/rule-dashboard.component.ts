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
import { ApprovalRule, RuleCondition, RuleAction, RuleDialogData } from '../../../../core/models';
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
        // Backend liefert rules mit conditions/actions als JSON-Strings
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

  private parseConditions(conditionsJson: string | undefined): RuleCondition[] {
    if (!conditionsJson) return [];
    try {
      return JSON.parse(conditionsJson);
    } catch {
      return [];
    }
  }

  private parseActions(actionsJson: string | undefined): RuleAction[] {
    if (!actionsJson) return [];
    try {
      return JSON.parse(actionsJson);
    } catch {
      return [];
    }
  }

  // Regel-Management
  onCreateRule() {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '1100px',
      maxWidth: '95vw',
      data: { mode: 'create' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const dto = this.buildRuleDto(result);

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
      width: '1100px',
      maxWidth: '95vw',
      data: { rule: { ...rule }, mode: 'edit' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const dto = this.buildRuleDto(result);

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
    const dto = this.buildRuleDto({ ...rule, isActive: newIsActive });

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

  private buildRuleDto(rule: any) {
    const ruleType = typeof rule.ruleType === 'number'
      ? (rule.ruleType === 0 ? 'automatic' : 'manual')
      : String(rule.ruleType || 'manual');

    const normalizeConditions = (rule.conditions || []).map((c: any) => ({
      ...c,
      value: c?.value !== undefined && c?.value !== null ? String(c.value) : ''
    }));

    const normalizeActions = (rule.actions || []).map((a: any) => ({
      ...a,
      value: a?.value !== undefined && a?.value !== null ? String(a.value) : ''
    }));

    return {
      name: rule.name,
      description: rule.description,
      ruleType,
      priority: rule.priority,
      conditions: JSON.stringify(normalizeConditions),
      actions: JSON.stringify(normalizeActions),
      isActive: rule.isActive
    };
  }
}
