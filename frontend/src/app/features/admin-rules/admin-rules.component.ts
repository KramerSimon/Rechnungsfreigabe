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
import { RuleDialogComponent, type RuleDialogData, type ApprovalRule, type RuleCondition, type RuleAction } from './rule-dialog.component';

export type { ApprovalRule, RuleCondition, RuleAction };

export interface CostCenter {
  id: string;
  name: string;
  description: string;
  manager: string;
}

export interface Project {
  id: string;
  name: string;
  costCenter: string;
  budget: number;
  status: string;
}

@Component({
  selector: 'app-admin-rules',
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
  templateUrl: './admin-rules.component.html',
  styleUrls: ['./admin-rules.component.scss']
})
export class AdminRulesComponent implements OnInit {
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
  costCenters: CostCenter[] = [
    { id: 'IT', name: 'IT-Abteilung', description: 'Informationstechnologie', manager: 'Hans Schmidt' },
    { id: 'HR', name: 'Personalabteilung', description: 'Human Resources', manager: 'Maria Müller' },
    { id: 'SALES', name: 'Vertrieb', description: 'Verkauf und Marketing', manager: 'Tom Wagner' },
    { id: 'FINANCE', name: 'Finanzen', description: 'Buchhaltung und Controlling', manager: 'Lisa Klein' },
    { id: 'OFFICE', name: 'Büromaterial', description: 'Allgemeine Büroausstattung', manager: 'Admin' }
  ];

  // Projekte
  projects: Project[] = [
    { id: 'WEB001', name: 'Website Relaunch', costCenter: 'IT', budget: 25000, status: 'Aktiv' },
    { id: 'HR002', name: 'Mitarbeiter-Portal', costCenter: 'HR', budget: 15000, status: 'Geplant' },
    { id: 'SALES003', name: 'CRM System', costCenter: 'SALES', budget: 40000, status: 'Aktiv' },
    { id: 'OFF004', name: 'Büroausstattung 2024', costCenter: 'OFFICE', budget: 5000, status: 'Aktiv' }
  ];

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

  constructor(private dialog: MatDialog) {}

  ngOnInit() {
    // Lade Daten beim Initialisieren
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

  // Kostenstellen-Management
  onAddCostCenter() {
    if (this.newCostCenter.id && this.newCostCenter.name) {
      this.costCenters.push({
        id: this.newCostCenter.id,
        name: this.newCostCenter.name,
        description: this.newCostCenter.description || '',
        manager: this.newCostCenter.manager || ''
      });
      this.newCostCenter = { id: '', name: '', description: '', manager: '' };
    }
  }

  onDeleteCostCenter(id: string) {
    if (confirm('Kostenstelle wirklich löschen?')) {
      this.costCenters = this.costCenters.filter(center => center.id !== id);
    }
  }

  // Projekt-Management
  onAddProject() {
    if (this.newProject.id && this.newProject.name && this.newProject.costCenter) {
      this.projects.push({
        id: this.newProject.id,
        name: this.newProject.name,
        costCenter: this.newProject.costCenter,
        budget: this.newProject.budget || 0,
        status: this.newProject.status || 'Geplant'
      });
      this.newProject = { id: '', name: '', costCenter: '', budget: 0, status: 'Geplant' };
    }
  }

  onDeleteProject(id: string) {
    if (confirm('Projekt wirklich löschen?')) {
      this.projects = this.projects.filter(project => project.id !== id);
    }
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
