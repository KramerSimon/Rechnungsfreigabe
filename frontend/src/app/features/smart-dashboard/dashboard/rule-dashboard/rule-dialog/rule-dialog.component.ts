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
    { value: 'require_approval', label: 'Freigabe erforderlich' },
    { value: 'set_status', label: 'Status setzen' },
    { value: 'assign_to', label: 'Zuweisen an' }
  ];

  constructor(
    public dialogRef: MatDialogRef<RuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RuleDialogData
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
    this.rule.actions.push({
      type: 'auto_approve',
      value: '',
      description: ''
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
        action.description = 'Freigabe erforderlich';
        action.value = 'pending';
        break;
      case 'set_status':
        action.description = 'Status setzen auf';
        action.value = '';
        break;
      case 'assign_to':
        action.description = 'Zuweisen an';
        action.value = '';
        break;
    }
  }

  isValid(): boolean {
    return !!(
      this.rule.name &&
      this.rule.ruleType &&
      this.rule.conditions.length > 0 &&
      this.rule.actions.length > 0 &&
      this.rule.conditions.every((c: RuleCondition) => c.field && c.operator && c.value !== '') &&
      this.rule.actions.every((a: RuleAction) => a.type && a.description)
    );
  }

  onCancel() {
    this.dialogRef.close();
  }

  onSave() {
    if (this.isValid()) {
      this.dialogRef.close(this.rule);
    }
  }
}
