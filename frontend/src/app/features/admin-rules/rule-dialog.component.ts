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
import { ApprovalRule, RuleCondition, RuleAction, RuleDialogData } from '../../core/models';

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
  template: `
    <h2 mat-dialog-title>
      <mat-icon>{{ data.mode === 'create' ? 'add' : 'edit' }}</mat-icon>
      {{ data.mode === 'create' ? 'Neue Regel erstellen' : 'Regel bearbeiten' }}
    </h2>

    <mat-dialog-content class="dialog-content">
      <!-- Basis-Informationen -->
      <div class="section">
        <h3>Basis-Informationen</h3>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Regelname</mat-label>
          <input matInput [(ngModel)]="rule.name" placeholder="z.B. Kleinstbeträge Auto-Freigabe" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Beschreibung</mat-label>
          <textarea matInput [(ngModel)]="rule.description" rows="3"
                   placeholder="Beschreibung der Regel und ihrer Auswirkungen"></textarea>
        </mat-form-field>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Regeltyp</mat-label>
            <mat-select [(ngModel)]="rule.ruleType" required>
              <mat-option value="automatic">Automatische Freigabe</mat-option>
              <mat-option value="manual">Manuelle Freigabe</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Priorität</mat-label>
            <input matInput type="number" [(ngModel)]="rule.priority" min="1" max="999">
          </mat-form-field>
        </div>

        <mat-checkbox [(ngModel)]="rule.isActive">Regel ist aktiv</mat-checkbox>
      </div>

      <!-- Bedingungen -->
      <div class="section">
        <h3>Bedingungen (WENN)</h3>
        <p class="section-description">Definieren Sie die Bedingungen, unter denen diese Regel greift.</p>

        <div class="conditions-list">
          <div *ngFor="let condition of rule.conditions; let i = index" class="condition-item">
            <div class="condition-row">
              <mat-form-field appearance="outline" *ngIf="i > 0">
                <mat-label>Verknüpfung</mat-label>
                <mat-select [(ngModel)]="condition.logicalOperator">
                  <mat-option value="AND">UND</mat-option>
                  <mat-option value="OR">ODER</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Feld</mat-label>
                <mat-select [(ngModel)]="condition.field" required>
                  <mat-option *ngFor="let field of availableFields" [value]="field.value">
                    {{ field.label }}
                  </mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Operator</mat-label>
                <mat-select [(ngModel)]="condition.operator" required>
                  <mat-option *ngFor="let op of availableOperators" [value]="op.value">
                    {{ op.label }}
                  </mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Wert</mat-label>
                <input matInput [(ngModel)]="condition.value" required>
              </mat-form-field>

              <button mat-icon-button color="warn" (click)="removeCondition(i)">
                <mat-icon>remove_circle</mat-icon>
              </button>
            </div>
          </div>
        </div>

        <button mat-button color="primary" (click)="addCondition()">
          <mat-icon>add</mat-icon>
          Bedingung hinzufügen
        </button>
      </div>

      <!-- Aktionen -->
      <div class="section">
        <h3>Aktionen (DANN)</h3>
        <p class="section-description">Definieren Sie, was passiert, wenn die Bedingungen erfüllt sind.</p>

        <div class="actions-list">
          <div *ngFor="let action of rule.actions; let i = index" class="action-item">
            <div class="action-row">
              <mat-form-field appearance="outline">
                <mat-label>Aktionstyp</mat-label>
                <mat-select [(ngModel)]="action.type" required (ngModelChange)="onActionTypeChange(action, $event)">
                  <mat-option *ngFor="let actionType of availableActions" [value]="actionType.value">
                    {{ actionType.label }}
                  </mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline" *ngIf="action.type === 'set_status' || action.type === 'assign_to'">
                <mat-label>Wert</mat-label>
                <input matInput [(ngModel)]="action.value" required>
              </mat-form-field>

              <mat-form-field appearance="outline" class="flex-grow">
                <mat-label>Beschreibung</mat-label>
                <input matInput [(ngModel)]="action.description" required>
              </mat-form-field>

              <button mat-icon-button color="warn" (click)="removeAction(i)">
                <mat-icon>remove_circle</mat-icon>
              </button>
            </div>
          </div>
        </div>

        <button mat-button color="primary" (click)="addAction()">
          <mat-icon>add</mat-icon>
          Aktion hinzufügen
        </button>
      </div>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Abbrechen</button>
      <button mat-raised-button color="primary" (click)="onSave()" [disabled]="!isValid()">
        {{ data.mode === 'create' ? 'Erstellen' : 'Speichern' }}
      </button>
    </mat-dialog-actions>
  `,
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
