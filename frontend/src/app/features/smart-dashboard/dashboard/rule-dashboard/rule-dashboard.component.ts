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
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';

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
    RouterModule
  ],
  templateUrl: './rule-dashboard.component.html',
  styleUrls: ['./rule-dashboard.component.scss']
})
export class RuleDashboardComponent implements OnInit {
  activeTab = 0;

  // Freigabe-Regeln
  approvalRules: ApprovalRule[] = [
    {
      id: 1,
      name: 'Kleinstbeträge (Automatische Freigabe)',
      description: 'WENN Betrag < 50,00 EUR UND Lieferant = "Büromaterial" DANN -> Sofort "Bezahlt" setzen',
      isActive: true,
      ruleType: 'automatic',
      priority: 1,
      conditions: [
        { field: 'amount', operator: '<', value: 50 },
        { field: 'supplier', operator: '=', value: 'Büromaterial', logicalOperator: 'AND' }
      ],
      actions: [
        { type: 'auto_approve', value: 'paid', description: 'Sofort als "Bezahlt" markieren' }
      ]
    },
    {
      id: 2,
      name: 'IT-Investitionen (Manuelle Freigabe)',
      description: 'WENN Kostenstelle = "IT" ODER Betrag > 500,00 EUR DANN -> Freigabe durch "Active Directory Manager"',
      isActive: true,
      ruleType: 'manual',
      priority: 2,
      conditions: [
        { field: 'costCenter', operator: '=', value: 'IT' },
        { field: 'amount', operator: '>', value: 500, logicalOperator: 'OR' }
      ],
      actions: [
        { type: 'require_approval', value: 'ad_manager', description: 'Freigabe durch Active Directory Manager erforderlich' }
      ]
    },
    {
      id: 3,
      name: 'Standard-Prozess',
      description: 'Alle anderen Rechnungen durchlaufen den Standard-Freigabeprozess',
      isActive: true,
      ruleType: 'manual',
      priority: 999,
      conditions: [],
      actions: [
        { type: 'require_approval', value: 'standard', description: 'Standard-Freigabeprozess' }
      ]
    }
  ];

  // Kostenstellen
  costCenters: CostCenter[] = [];

  // Projekte
  projects: Project[] = [];

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
    manager: ''
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
    { value: 'require_approval', label: 'Freigabe erforderlich' },
    { value: 'set_status', label: 'Status setzen' },
    { value: 'assign_to', label: 'Zuweisen an' }
  ];

  constructor(private dialog: MatDialog, private costCenterService: CostCenterService, private projectService: ProjectService) {}

  ngOnInit() {
    this.costCenterService.getCostCenters().subscribe((centers) => {
      this.costCenters = centers;
    });
    this.projectService.getProjects().subscribe((projects) => {
      this.projects = projects;
    });
  }

  // Regel-Management
  onCreateRule() {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { mode: 'create' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const newRule = {
          ...result,
          id: Math.max(...this.approvalRules.map(r => r.id), 0) + 1
        } as ApprovalRule;
        this.approvalRules.push(newRule);
        console.log('Neue Regel erstellt:', newRule);
      }
    });
  }

  onEditRule(rule: ApprovalRule) {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { rule: { ...rule }, mode: 'edit' } as RuleDialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const index = this.approvalRules.findIndex(r => r.id === rule.id);
        if (index !== -1) {
          this.approvalRules[index] = { ...result, id: rule.id };
          console.log('Regel bearbeitet:', result);
        }
      }
    });
  }

  onDeleteRule(ruleId: number) {
    if (confirm('Regel wirklich löschen?')) {
      this.approvalRules = this.approvalRules.filter(rule => rule.id !== ruleId);
    }
  }

  onToggleRule(rule: ApprovalRule) {
    rule.isActive = !rule.isActive;
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
    return actions.map(action => action.description).join(', ');
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
}
