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
import { ApprovalRule, RuleCondition, RuleAction, RuleDialogData } from '../../../../../core/models';
import { User } from '../../../../../core/models/user.models';
import { UserService } from '../../../../../core/services/user.service';

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

  availableActions = [
    { value: 'auto_approve', label: 'Automatisch freigeben' },
    { value: 'set_status', label: 'Status setzen' },
    { value: 'assign_to', label: 'Zuweisen an' }
  ];

  getActionOptions() {
    // Manual: no auto_approve; Automatic: no assign_to
    return this.rule.ruleType === 'automatic'
      ? this.availableActions.filter(a => a.value !== 'assign_to')
      : this.availableActions.filter(a => a.value !== 'auto_approve');
  }

  constructor(
    public dialogRef: MatDialogRef<RuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RuleDialogData,
    private userService: UserService
  ) {
    this.rule = data.rule ? { ...data.rule } : this.createEmptyRule();
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
      priority: 10
    };
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
      case 'set_status':
        action.description = 'Status setzen auf';
        action.value = '';
        break;
      case 'assign_to':
        action.description = 'Zuweisen an Benutzer';
        action.value = '';
        break;
    }
  }

  onRuleTypeChange(newType: string) {
    this.rule.ruleType = newType as any;
    // Ensure manual rules have at least one assign_to action
    if (newType === 'manual') {
      // Remove invalid actions for manual rules
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'auto_approve');
      const hasAssign = this.rule.actions.some(a => a.type === 'assign_to');
      if (!hasAssign) {
        this.rule.actions.unshift({ type: 'assign_to', value: '', description: 'Zuweisen an Benutzer' } as any);
      }
    }
    // For automatic rules, set first action to auto_approve if none
    if (newType === 'automatic' && this.rule.actions.length === 0) {
      this.rule.actions.push({ type: 'auto_approve', value: 'approved', description: 'Automatisch freigeben' } as any);
    }
    if (newType === 'automatic') {
      // Remove invalid actions for automatic rules
      this.rule.actions = this.rule.actions.filter(a => a.type !== 'assign_to');
      const hasAuto = this.rule.actions.some(a => a.type === 'auto_approve');
      if (!hasAuto) {
        this.rule.actions.unshift({ type: 'auto_approve', value: 'approved', description: 'Automatisch freigeben' } as any);
      }
    }
  }

  isValid(): boolean {
    const baseValid = !!(
      this.rule.name &&
      this.rule.ruleType &&
      this.rule.conditions.length > 0 &&
      this.rule.actions.length > 0 &&
      this.rule.conditions.every((c: RuleCondition) => c.field && c.operator && c.value !== '') &&
      this.rule.actions.every((a: RuleAction) => {
        if (!a.type || !a.description) return false;
        if (a.type === 'set_status') return a.value !== '';
        if (a.type === 'assign_to') return a.value !== '';
        return true;
      })
    );

    // Manual rules must assign to a specific user
    if (this.rule.ruleType === 'manual') {
      const hasAssign = this.rule.actions.some(a => a.type === 'assign_to' && a.value !== '');
      if (!hasAssign) return false;
    }

    // Automatic rules must auto-approve
    if (this.rule.ruleType === 'automatic') {
      const hasAuto = this.rule.actions.some(a => a.type === 'auto_approve');
      if (!hasAuto) return false;
    }

    return baseValid;
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
