import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CreateEscalationRuleDto, EscalationRule, StatusDto } from '../../../core/models/escalation-rule.model';
import { RoleDto, User } from '../../../core/models/user.models';

export interface EscalationRuleDialogData {
  mode: 'create' | 'edit';
  statuses: StatusDto[];
  roles: RoleDto[];
  users: User[];
  rule?: EscalationRule;
}

@Component({
  selector: 'app-escalation-rule-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Neue Eskalationsregel' : 'Eskalationsregel bearbeiten' }}</h2>

      <form [formGroup]="form" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Name *</mat-label>
            <input matInput formControlName="name" placeholder="z.B. Eskalation nach 48h">
            <mat-error *ngIf="form.get('name')?.hasError('required')">Name ist erforderlich</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Status-Auslöser (mehrfach möglich) *</mat-label>
            <mat-select formControlName="triggerStatusIds" multiple>
              <mat-select-trigger>
                <span class="tag-list">
                  <span
                    *ngFor="let statusId of form.get('triggerStatusIds')?.value"
                    class="status-tag"
                    [style.backgroundColor]="getStatusById(statusId)?.color || '#9E9E9E'">
                    {{getStatusById(statusId)?.displayName || statusId}}
                  </span>
                </span>
              </mat-select-trigger>
              <mat-option *ngFor="let status of data.statuses" [value]="status.id">
                <span class="status-tag" [style.backgroundColor]="status.color || '#9E9E9E'">
                  {{status.displayName}}
                </span>
              </mat-option>
            </mat-select>
            <mat-error *ngIf="form.get('triggerStatusIds')?.hasError('required')">Mindestens ein Status ist erforderlich</mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="time-field">
            <mat-label>Tage</mat-label>
            <input matInput type="number" formControlName="triggerDays" min="0" max="10">
          </mat-form-field>

          <mat-form-field appearance="outline" class="time-field">
            <mat-label>Stunden</mat-label>
            <input matInput type="number" formControlName="triggerHours" min="0" max="23">
          </mat-form-field>

          <mat-form-field appearance="outline" class="time-field">
            <mat-label>Minuten</mat-label>
            <input matInput type="number" formControlName="triggerMinutes" min="0" max="59">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Wiederholung alle (Stunden)</mat-label>
            <input matInput type="number" formControlName="repeatIntervalHours" min="1">
            <mat-hint>Optional. Wenn leer, wird nicht wiederholt.</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Max. Eskalationen</mat-label>
            <input matInput type="number" formControlName="maxEscalations" min="0">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Benachrichtigte Rollen (mehrfach möglich)</mat-label>
            <mat-select formControlName="notifyRoleIds" multiple>
              <mat-select-trigger>
                <span class="tag-list">
                  <span
                    *ngFor="let roleId of form.get('notifyRoleIds')?.value"
                    class="role-tag"
                    [style.backgroundColor]="getRoleById(roleId)?.color || '#ff9800'">
                    {{getRoleById(roleId)?.name || roleId}}
                  </span>
                </span>
              </mat-select-trigger>
              <mat-option *ngFor="let role of data.roles" [value]="role.id">
                <span class="role-tag" [style.backgroundColor]="role.color || '#ff9800'">
                  {{role.name}}
                </span>
              </mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Benutzer (mehrfach möglich)</mat-label>
            <mat-select formControlName="notifyUserIds" multiple>
              <mat-option *ngFor="let user of data.users" [value]="user.id">{{user.firstName}} {{user.lastName}} ({{user.username}})</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Nachrichtenvorlage</mat-label>
            <textarea matInput formControlName="messageTemplate" rows="4"
              placeholder="Die Standard-Vorlage enthält bereits Rechnungs- und Workflow-Informationen. Geben Sie hier zusätzlichen Text ein, falls gewünscht."></textarea>
            <mat-hint>Verfügbare Platzhalter: {{dQ}}InvoiceNumber{{dQ}}, {{dQ}}SupplierName{{dQ}}, {{dQ}}Amount{{dQ}}, {{dQ}}Status{{dQ}}, {{dQ}}RuleName{{dQ}}</mat-hint>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="2"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-checkbox formControlName="isActive">Regel aktiv</mat-checkbox>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary" (click)="onSave()" [disabled]="form.invalid">
          {{ data.mode === 'create' ? 'Erstellen' : 'Speichern' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 100%;
      padding: 0 16px;
      overflow-x: hidden;
    }

    .form-row {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 16px;
      margin-bottom: 16px;
      align-items: flex-start;
    }

    .form-row mat-form-field {
      width: 100%;
    }

    .time-field {
      flex: 1;
      min-width: 80px;
    }

    .full-width {
      grid-column: 1 / -1;
      width: 100%;
    }

    mat-checkbox {
      margin-bottom: 12px;
      grid-column: 1 / -1;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 16px 0 0 0;
      flex-wrap: wrap;
    }

    textarea {
      resize: vertical;
    }

    /* Status and Role tags in dropdown */
    .status-tag,
    .role-tag {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 13px;
      font-weight: 500;
      color: white;
      white-space: nowrap;
    }

    .tag-list {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      min-height: 24px;
    }

    /* Time fields in a row */
    .form-row:has(.time-field) {
      grid-template-columns: repeat(3, 1fr);
    }

    /* Mobile screens (up to 768px) */
    @media (max-width: 768px) {
      .dialog-container {
        padding: 0 12px;
      }

      .form-row {
        grid-template-columns: 1fr;
        gap: 12px;
        margin-bottom: 12px;
      }

      .form-row:has(.time-field) {
        grid-template-columns: repeat(3, 1fr);
      }

      .dialog-actions {
        gap: 6px;
      }

      .dialog-actions button {
        flex: 1;
      }
    }

    /* Small mobile screens (up to 480px) */
    @media (max-width: 480px) {
      .dialog-container {
        padding: 0 8px;
      }

      .form-row {
        gap: 8px;
        margin-bottom: 8px;
      }

      .dialog-actions {
        flex-direction: column-reverse;
        gap: 8px;
        padding-top: 12px;
      }

      .dialog-actions button {
        width: 100%;
      }

      h2 {
        font-size: 18px;
        margin-bottom: 12px;
      }
    }
  `]
})
export class EscalationRuleDialogComponent {
  form: FormGroup;
  dQ = '{'; // for displaying curly braces in template hints

  constructor(
    private dialogRef: MatDialogRef<EscalationRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EscalationRuleDialogData,
    private fb: FormBuilder,
  ) {
    const rule = data.rule;

    // Convert minutes to days, hours, minutes for editing
    const totalMinutes = rule?.triggerAfterMinutes ?? 2880; // Default: 48 hours
    const days = Math.floor(totalMinutes / 1440);
    const hours = Math.floor((totalMinutes % 1440) / 60);
    const minutes = totalMinutes % 60;

    this.form = this.fb.group({
      name: [rule?.name || '', Validators.required],
      description: [rule?.description || ''],
      triggerStatusIds: [rule?.triggerStatusIds || (data.statuses.length > 0 ? [data.statuses[0].id] : []), Validators.required],
      triggerDays: [days, [Validators.min(0), Validators.max(10)]],
      triggerHours: [hours, [Validators.min(0), Validators.max(23)]],
      triggerMinutes: [minutes, [Validators.min(0), Validators.max(59)]],
      repeatIntervalHours: [rule?.repeatIntervalHours ?? null, [Validators.min(1)]],
      maxEscalations: [rule?.maxEscalations ?? 3, [Validators.min(0)]],
      notifyRoleIds: [rule?.notifyRoleIds || []],
      notifyUserIds: [rule?.notifyUserIds || []],
      messageTemplate: [rule?.messageTemplate || ''],
      isActive: [rule?.isActive ?? true],
    });
  }

  getStatusById(statusId: number | string): StatusDto | undefined {
    return this.data.statuses.find(status => String(status.id) === String(statusId));
  }

  getRoleById(roleId: number | string): RoleDto | undefined {
    return this.data.roles.find(role => String(role.id) === String(roleId));
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.invalid) {
      return;
    }

    // Convert days, hours, minutes to total minutes
    const days = Number(this.form.value.triggerDays) || 0;
    const hours = Number(this.form.value.triggerHours) || 0;
    const minutes = Number(this.form.value.triggerMinutes) || 0;
    const totalMinutes = days * 1440 + hours * 60 + minutes;

    // Validation: at least 1 minute required
    if (totalMinutes < 1) {
      return;
    }

    const payload: CreateEscalationRuleDto = {
      name: this.form.value.name,
      description: this.form.value.description,
      triggerStatusIds: this.form.value.triggerStatusIds || [],
      triggerAfterMinutes: totalMinutes,
      repeatIntervalHours: this.form.value.repeatIntervalHours === null || this.form.value.repeatIntervalHours === ''
        ? null
        : Number(this.form.value.repeatIntervalHours),
      maxEscalations: this.form.value.maxEscalations === null || this.form.value.maxEscalations === ''
        ? null
        : Number(this.form.value.maxEscalations),
      notifyRoleIds: this.form.value.notifyRoleIds || [],
      notifyUserIds: this.form.value.notifyUserIds || [],
      messageTemplate: this.form.value.messageTemplate,
      isActive: this.form.value.isActive,
    };

    this.dialogRef.close(payload);
  }
}

