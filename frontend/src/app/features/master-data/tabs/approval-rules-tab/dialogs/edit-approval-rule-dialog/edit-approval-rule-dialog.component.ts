import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { SupplierService } from '../../../../../../core/services/supplier.service';
import { CostCenterService } from '../../../../../../core/services/cost-center.service';
import { ProjectService } from '../../../../../../core/services/project.service';
import { StatusService } from '../../../../../../core/services/status.service';
import { RoleService } from '../../../../../../core/services/role.service';
import { UserService } from '../../../../../../core/services/user.service';
import { Supplier } from '../../../../../../core/models/supplier.model';
import { CostCenter } from '../../../../../../core/models/cost-center.model';
import { Project } from '../../../../../../core/models/project.model';
import { Status } from '../../../../../../core/models/status.model';
import { RoleDto, User } from '../../../../../../core/models/user.models';

@Component({
  selector: 'app-edit-approval-rule-dialog',
  templateUrl: './edit-approval-rule-dialog.component.html',
  styleUrls: ['./edit-approval-rule-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule,
    MatIconModule,
    MatChipsModule
  ]
})
export class EditApprovalRuleDialogComponent implements OnInit {
  rule: DialogRule;
  users: User[] = [];
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];
  statusOptions: Status[] = [];
  roles: RoleDto[] = [];

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

  constructor(
    public dialogRef: MatDialogRef<EditApprovalRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogRule,
    private userService: UserService,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService,
    private statusService: StatusService,
    private roleService: RoleService
  ) {
    this.rule = data
      ? {
          ...data,
          ruleType: this.normalizeRuleType(data.ruleType),
          conditions: data.conditions || [],
          actions: data.actions || []
        }
      : this.createEmptyRule();
  }

  ngOnInit(): void {
    if (this.rule.conditions.length === 0) {
      this.addCondition();
    }
    if (this.rule.actions.length === 0) {
      this.addAction();
    }

    this.userService.getUsers().subscribe({
      next: (users) => (this.users = users || []),
      error: () => (this.users = [])
    });

    this.loadSuppliers();
    this.loadCostCenters();
    this.loadProjects();

    this.statusService.getInvoiceStatuses().subscribe({
      next: (statuses) => {
        this.statusOptions = statuses || [];
      },
      error: () => {
        this.statusOptions = [];
      }
    });

    this.roleService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles || [];
        this.syncStageRoleIds();
      },
      error: () => {
        this.roles = [];
      }
    });
  }

  loadSuppliers(): void {
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
        this.clearDropdownCache();
      },
      error: (error) => {
        console.error('Fehler beim Laden der Lieferanten:', error);
      }
    });
  }

  loadCostCenters(): void {
    this.costCenterService.getCostCenters().subscribe({
      next: (costCenters) => {
        this.costCenters = costCenters;
        this.clearDropdownCache();
      },
      error: (error) => {
        console.error('Fehler beim Laden der Kostenstellen:', error);
      }
    });
  }

  loadProjects(): void {
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.clearDropdownCache();
      },
      error: (error) => {
        console.error('Fehler beim Laden der Projekte:', error);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.isValid()) {
      this.dialogRef.close(this.rule);
    }
  }

  getActionOptions() {
    return this.rule.ruleType === 'automatic'
      ? this.availableActions.filter(a => a.value !== 'assign_to')
      : this.availableActions.filter(a => a.value !== 'auto_approve');
  }

  private createEmptyRule(): DialogRule {
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
    if (newType === 'manual') {
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'auto_approve');
    }
    if (newType === 'automatic') {
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'assign_to');
    }
  }

  isValid(): boolean {
    const setStatusCount = this.rule.actions.filter(a => a.type === 'set_status').length;
    const baseValid = !!(
      this.rule.name &&
      this.rule.ruleType &&
      this.rule.conditions.length > 0 &&
      this.rule.actions.length > 0 &&
      this.rule.conditions.every((c: RuleCondition) => c.field && c.operator && c.value !== '') &&
      this.rule.actions.every((a: RuleAction) => this.isActionValid(a))
    );

    return baseValid && setStatusCount <= 1;
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
    return true;
  }

  private isStageValid(stage: StageDefinition): boolean {
    if (!stage) return false;
    if (stage.stepNumber === undefined || stage.stepNumber === null) return false;
    if (stage.approvalLevel === undefined || stage.approvalLevel === null) return false;
    const hasRoleOrUser = !!stage.roleId || !!stage.userId;
    return stage.stepNumber >= 1 && stage.approvalLevel >= 1 && hasRoleOrUser;
  }

  addStage(action: RuleAction) {
    if (action.type !== 'require_approval') return;
    if (!action.stages) action.stages = [];
    const nextStep = (action.stages[action.stages.length - 1]?.stepNumber || 0) + 1;
    const nextLevel = (action.stages[action.stages.length - 1]?.approvalLevel || 0) + 1;
    action.stages.push({ stepNumber: nextStep, approvalLevel: nextLevel, roleId: this.roles[0]?.id });
  }

  removeStage(action: RuleAction, index: number) {
    if (action.type !== 'require_approval' || !action.stages) return;
    action.stages.splice(index, 1);
  }

  private createDefaultStage(): StageDefinition {
    return { stepNumber: 1, approvalLevel: 1, roleId: this.roles[0]?.id };
  }

  private dropdownOptionsCache: Map<string, { value: any; label: string }[]> = new Map();
  private operatorsCache: Map<string, { value: string; label: string }[]> = new Map();

  isDropdownField(field: string): boolean {
    return ['supplier', 'costCenter', 'project'].includes(field);
  }

  isDateField(field: string): boolean {
    return ['invoiceDate', 'dueDate'].includes(field);
  }

  isAmountField(field: string): boolean {
    return field === 'amount';
  }

  getAvailableOperators(field: string): { value: string; label: string }[] {
    if (this.operatorsCache.has(field)) {
      return this.operatorsCache.get(field)!;
    }

    let operators: { value: string; label: string }[];
    if (this.isDropdownField(field)) {
      operators = this.availableOperators.filter(op => ['=', '!='].includes(op.value));
    } else if (this.isDateField(field)) {
      operators = this.dateOperators;
    } else {
      operators = this.availableOperators;
    }

    this.operatorsCache.set(field, operators);
    return operators;
  }

  getDropdownOptions(field: string): { value: any; label: string }[] {
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

    this.dropdownOptionsCache.set(field, options);
    return options;
  }

  private clearDropdownCache(): void {
    this.dropdownOptionsCache.clear();
  }

  isActionTypeDisabled(action: RuleAction, type: string): boolean {
    if (type !== 'set_status') return false;
    return this.rule.actions.some(a => a !== action && a.type === 'set_status');
  }

  getStatusByCode(code?: string | null): Status | undefined {
    if (!code) return undefined;
    return this.statusOptions.find(s => s.code === code);
  }

  getStatusBadgeStyles(status?: Status): { [key: string]: string | null } {
    const color = status?.color || '#9e9e9e';
    return {
      'background-color': color,
      color: '#fff',
      'border-color': null
    };
  }

  getRoleById(id?: number | null): RoleDto | undefined {
    if (!id) return undefined;
    return this.roles.find(r => r.id === id);
  }

  getRoleBadgeStyles(role?: RoleDto): { [key: string]: string | null } {
    const color = role?.color || '#9e9e9e';
    return {
      'background-color': color,
      color: '#fff',
      'border-color': null
    };
  }

  private syncStageRoleIds(): void {
    if (!this.roles.length) return;
    this.rule.actions.forEach(action => {
      if (!action.stages) return;
      action.stages.forEach(stage => {
        if (stage.roleId) return;
        const roleName = (stage as any).role as string | undefined;
        if (!roleName) return;
        const match = this.roles.find(r => r.name === roleName);
        if (match) {
          stage.roleId = match.id;
        }
      });
    });
  }
}

interface DialogRule {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  ruleType: 'automatic' | 'manual' | 'Automatic' | 'Manual' | string;
  conditions: RuleCondition[];
  actions: RuleAction[];
  priority: number;
  supplierId?: number | null;
  costCenterId?: string | null;
  projectId?: string | null;
}

interface RuleCondition {
  field: string;
  operator: string;
  value: string | number;
  logicalOperator?: 'AND' | 'OR';
}

interface RuleAction {
  type: 'auto_approve' | 'require_approval' | 'set_status' | 'assign_to';
  value: string;
  description: string;
  stages?: StageDefinition[];
}

interface StageDefinition {
  stepNumber: number;
  approvalLevel: number;
  roleId?: number | null;
  userId?: number | null;
}
