import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { ApprovalRule, RuleCondition, RuleAction, RuleDialogData, StageDefinition } from '../../../../../core/models';
import { User } from '../../../../../core/models/user.models';
import { UserService } from '../../../../../core/services/user.service';
import { SupplierService } from '../../../../../core/services/supplier.service';
import { CostCenterService } from '../../../../../core/services/cost-center.service';
import { ProjectService } from '../../../../../core/services/project.service';
import { Supplier } from '../../../../../core/models/supplier.model';
import { CostCenter } from '../../../../../core/models/cost-center.model';
import { Project } from '../../../../../core/models/project.model';

@Component({
  selector: 'app-rule-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    MatChipsModule
  ],
  templateUrl: `./rule-dialog.component.html`,
  styleUrls: ['./rule-dialog.component.scss']
})
export class RuleDialogComponent implements OnInit {
  rule: ApprovalRule;
  users: User[] = [];
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];

  availableFields = [
    { value: 'amount', label: 'Betrag' },
    { value: 'supplier', label: 'Lieferant' },
    { value: 'costCenter', label: 'Kostenstelle' },
    { value: 'project', label: 'Projekt' },
    { value: 'invoiceDate', label: 'Rechnungsdatum' },
    { value: 'dueDate', label: 'Fälligkeitsdatum' }
  ];

  availableOperators = [
    { value: '=', label: 'gleich' },
    { value: '!=', label: 'ungleich' },
    { value: '>', label: 'größer als' },
    { value: '<', label: 'kleiner als' },
    { value: '>=', label: 'größer oder gleich' },
    { value: '<=', label: 'kleiner oder gleich' },
    { value: 'contains', label: 'enthält' }
  ];

  dateOperators = [
    { value: '<', label: 'vor' },
    { value: '<=', label: 'vor oder am' },
    { value: '=', label: 'am' },
    { value: '>=', label: 'am oder nach' },
    { value: '>', label: 'nach' },
    { value: '!=', label: 'ungleich' }
  ];

  availableActions = [
    { value: 'auto_approve', label: 'Automatisch freigeben' },
    { value: 'require_approval', label: 'Mehrstufige Freigabe' },
    { value: 'set_status', label: 'Status setzen' },
    { value: 'assign_to', label: 'Zuweisen an' }
  ];

  stageRoles = [
    { value: 'cost_center_manager', label: 'Kostenstellenleiter' },
    { value: 'project_manager', label: 'Projektleiter' },
    { value: 'manager', label: 'Manager' },
    { value: 'admin', label: 'Admin' }
  ];

  getActionOptions() {
    // Manual: no auto_approve; Automatic: allow all but assign_to if you want stricter separation
    return this.rule.ruleType === 'automatic'
      ? this.availableActions.filter(a => a.value !== 'assign_to')
      : this.availableActions.filter(a => a.value !== 'auto_approve');
  }

  constructor(
    public dialogRef: MatDialogRef<RuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RuleDialogData,
    private userService: UserService,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService
  ) {
    this.rule = data.rule ? { ...data.rule, ruleType: this.normalizeRuleType(data.rule.ruleType) } : this.createEmptyRule();
  }

  ngOnInit() {
    // Ensure we have at least one condition and one action for new rules
    if (this.data.mode === 'create') {
      if (this.rule.conditions.length === 0) {
        this.addCondition();
      }
      if (this.rule.actions.length === 0) {
        this.addAction();
      }
    }

    // Load users for assign_to action
    this.userService.getUsers().subscribe({
      next: (users) => (this.users = users || []),
      error: () => (this.users = [])
    });

    // Load suppliers for conditions
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers || [];
        this.clearDropdownCache();
      },
      error: (err) => {
        this.suppliers = [];
      }
    });

    // Load cost centers for conditions
    this.costCenterService.getCostCenters().subscribe({
      next: (costCenters) => {
        this.costCenters = costCenters || [];
        this.clearDropdownCache();
      },
      error: (err) => {
        this.costCenters = [];
      }
    });

    // Load projects for conditions
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects || [];
        this.clearDropdownCache();
      },
      error: (err) => {
        this.projects = [];
      }
    });
  }

  private createEmptyRule(): ApprovalRule {
    return {
      id: 0,
      name: '',
      description: '',
      isActive: true,
      ruleType: 'manual',
      conditions: [],
      actions: [],
      priority: 10,
      supplierId: null,
      costCenterId: null,
      projectId: null
    };
  }

  private normalizeRuleType(value: string): 'automatic' | 'manual' {
    return (value || '').toLowerCase() === 'automatic' ? 'automatic' : 'manual';
  }

  addCondition() {
    this.rule.conditions.push({
      field: 'amount',
      operator: '=',
      value: '',
      logicalOperator: this.rule.conditions.length > 0 ? 'AND' : undefined
    });
  }

  removeCondition(index: number) {
    this.rule.conditions.splice(index, 1);
  }

  addAction() {
    const isAutomatic = this.rule.ruleType === 'automatic';
    this.rule.actions.push({
      type: (isAutomatic ? 'auto_approve' : 'assign_to') as any,
      value: '',
      description: isAutomatic ? 'Automatisch freigeben' : 'Zuweisen an Benutzer'
    });
  }

  removeAction(index: number) {
    this.rule.actions.splice(index, 1);
  }

  onActionTypeChange(action: RuleAction, type: string) {
    action.type = type as any;
    // Update description based on type
    switch (type) {
      case 'auto_approve':
        action.description = 'Automatisch freigeben';
        action.value = 'approved';
        break;
      case 'require_approval':
        action.description = 'Mehrstufige Freigabe';
        action.value = '';
        action.stages = action.stages && action.stages.length ? action.stages : [this.createDefaultStage()];
        break;
      case 'set_status':
        action.description = 'Status setzen auf';
        action.value = '';
        action.stages = undefined;
        break;
      case 'assign_to':
        action.description = 'Zuweisen an Benutzer';
        action.value = '';
        action.stages = undefined;
        break;
    }
  }

  onRuleTypeChange(newType: string) {
    this.rule.ruleType = newType as any;
    // Clean up actions that are incompatible per mode
    if (newType === 'manual') {
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'auto_approve');
    }
    if (newType === 'automatic') {
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'assign_to');
    }
  }

  isValid(): boolean {
    const baseValid = !!(
      this.rule.name &&
      this.rule.ruleType &&
      this.rule.conditions.length > 0 &&
      this.rule.actions.length > 0 &&
      this.rule.conditions.every((c: RuleCondition) => c.field && c.operator && c.value !== '') &&
      this.rule.actions.every((a: RuleAction) => this.isActionValid(a))
    );

    return baseValid;
  }

  private isActionValid(action: RuleAction): boolean {
    if (!action.type || !action.description) return false;
    if (action.type === 'set_status') return action.value !== '';
    if (action.type === 'assign_to') return action.value !== '';
    if (action.type === 'require_approval') {
      const stages = action.stages || [];
      if (!stages.length) return false;
      return stages.every(s => this.isStageValid(s));
    }
    return true; // auto_approve has no extra validation beyond presence
  }

  private isStageValid(stage: StageDefinition): boolean {
    if (!stage) return false;
    if (stage.stepNumber === undefined || stage.stepNumber === null) return false;
    if (stage.approvalLevel === undefined || stage.approvalLevel === null) return false;
    const hasRoleOrUser = !!stage.role || !!stage.userId;
    return stage.stepNumber >= 1 && stage.approvalLevel >= 1 && hasRoleOrUser;
  }

  addStage(action: RuleAction) {
    if (action.type !== 'require_approval') return;
    if (!action.stages) action.stages = [];
    const nextStep = (action.stages[action.stages.length - 1]?.stepNumber || 0) + 1;
    const nextLevel = (action.stages[action.stages.length - 1]?.approvalLevel || 0) + 1;
    action.stages.push({ stepNumber: nextStep, approvalLevel: nextLevel, role: 'manager' });
  }

  removeStage(action: RuleAction, index: number) {
    if (action.type !== 'require_approval' || !action.stages) return;
    action.stages.splice(index, 1);
  }

  private createDefaultStage(): StageDefinition {
    return { stepNumber: 1, approvalLevel: 1, role: 'cost_center_manager' };
  }

  // Cache für Dropdown-Optionen
  private dropdownOptionsCache: Map<string, { value: any; label: string }[]> = new Map();
  private operatorsCache: Map<string, { value: string; label: string }[]> = new Map();

  isDropdownField(field: string): boolean {
    return ['supplier', 'costCenter', 'project'].includes(field);
  }

  isDateField(field: string): boolean {
    return ['invoiceDate', 'dueDate'].includes(field);
  }

  getAvailableOperators(field: string): { value: string; label: string }[] {
    // Cache prüfen
    if (this.operatorsCache.has(field)) {
      return this.operatorsCache.get(field)!;
    }

    // Für Dropdown-Felder (Lieferant, Kostenstelle, Projekt) nur = und !=
    // Für Datumsfelder spezielle Bezeichnungen (vor, nach, am, ...)
    let operators: { value: string; label: string }[];
    if (this.isDropdownField(field)) {
      operators = this.availableOperators.filter(op => ['=', '!='].includes(op.value));
    } else if (this.isDateField(field)) {
      operators = this.dateOperators;
    } else {
      operators = this.availableOperators;
    }

    // Im Cache speichern
    this.operatorsCache.set(field, operators);
    return operators;
  }

  getDropdownOptions(field: string): { value: any; label: string }[] {
    // Cache prüfen
    if (this.dropdownOptionsCache.has(field)) {
      return this.dropdownOptionsCache.get(field)!;
    }

    let options: { value: any; label: string }[] = [];

    switch (field) {
      case 'supplier':
        options = this.suppliers.map(s => ({ value: s.id.toString(), label: s.name }));
        break;
      case 'costCenter':
        options = this.costCenters.map(cc => ({ value: cc.id, label: cc.name }));
        break;
      case 'project':
        options = this.projects.map(p => ({ value: p.id, label: p.name }));
        break;
      default:
        options = [];
    }

    // Im Cache speichern
    this.dropdownOptionsCache.set(field, options);
    return options;
  }

  // Cache leeren wenn sich Daten ändern
  private clearDropdownCache(): void {
    this.dropdownOptionsCache.clear();
  }

  onCancel() {
    this.dialogRef.close();
  }

  onSave() {
    console.log('Saving rule:', this.rule);
    if (this.isValid()) {
      console.log('Rule is valid:', this.rule);
      this.dialogRef.close(this.rule);
    }
  }
}
