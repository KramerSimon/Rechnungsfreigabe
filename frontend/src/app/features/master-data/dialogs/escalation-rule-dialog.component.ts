import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CreateEscalationRuleDto, EscalationRule } from '../../../core/models/escalation-rule.model';
import { RoleDto, User } from '../../../core/models/user.models';

export interface EscalationRuleDialogData {
  mode: 'create' | 'edit';
  statuses: string[];
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
            <mat-label>Status-Auslöser *</mat-label>
            <mat-select formControlName="triggerStatus">
              <mat-option *ngFor="let status of data.statuses" [value]="status">{{status}}</mat-option>
            </mat-select>
            <mat-error *ngIf="form.get('triggerStatus')?.hasError('required')">Status ist erforderlich</mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Auslöser nach (Stunden)</mat-label>
            <input matInput type="number" formControlName="triggerAfterHours" min="1">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Wiederholung alle (Stunden)</mat-label>
            <input matInput type="number" formControlName="repeatIntervalHours" min="1">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Max. Eskalationen</mat-label>
            <input matInput type="number" formControlName="maxEscalations" min="0">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Benachrichtigte Rolle</mat-label>
            <mat-select formControlName="notifyRoleId">
              <mat-option [value]="null">- Keine Rolle -</mat-option>
              <mat-option *ngFor="let role of data.roles" [value]="role.id">{{role.name}}</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Benutzer (optional)</mat-label>
            <mat-select formControlName="notifyUserId">
              <mat-option [value]="null">- Kein Benutzer -</mat-option>
              <mat-option *ngFor="let user of data.users" [value]="user.id">{{user.firstName}} {{user.lastName}} ({{user.username}})</mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Nachrichtenvorlage</mat-label>
            <textarea matInput formControlName="messageTemplate" rows="3" placeholder="Freitext, z.B. Erinnerungstext"></textarea>
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
    :host ::ng-deep .escalation-rule-dialog {
      width: 80vw !important;
      max-width: 900px !important;
    }

    .dialog-container {
      width: 100%;
      padding: 0 16px;
      overflow-x: hidden;
    }

    .form-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 16px;
      margin-bottom: 16px;
      align-items: flex-start;
    }

    .form-row mat-form-field {
      width: 100%;
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

    /* Large screens */
    @media (min-width: 1200px) {
      .form-row {
        grid-template-columns: repeat(4, 1fr);
      }

      .form-row mat-form-field:nth-child(1) {
        grid-column: span 2;
      }

      .form-row mat-form-field:nth-child(2) {
        grid-column: span 2;
      }
    }

    /* Tablet screens (768px - 1199px) */
    @media (max-width: 1199px) and (min-width: 769px) {
      .form-row {
        grid-template-columns: repeat(2, 1fr);
      }
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

  constructor(
    private dialogRef: MatDialogRef<EscalationRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EscalationRuleDialogData,
    private fb: FormBuilder,
  ) {
    const rule = data.rule;
    this.form = this.fb.group({
      name: [rule?.name || '', Validators.required],
      description: [rule?.description || ''],
      triggerStatus: [rule?.triggerStatus || data.statuses[0], Validators.required],
      triggerAfterHours: [rule?.triggerAfterHours ?? 48, [Validators.required, Validators.min(1)]],
      repeatIntervalHours: [rule?.repeatIntervalHours ?? null, [Validators.min(1)]],
      maxEscalations: [rule?.maxEscalations ?? 3, [Validators.min(0)]],
      notifyRoleId: [rule?.notifyRoleId ?? null],
      notifyUserId: [rule?.notifyUserId ?? null],
      messageTemplate: [rule?.messageTemplate || ''],
      isActive: [rule?.isActive ?? true],
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.invalid) {
      return;
    }

    const payload: CreateEscalationRuleDto = {
      ...this.form.value,
      notifyUserId: this.form.value.notifyUserId === null || this.form.value.notifyUserId === ''
        ? null
        : Number(this.form.value.notifyUserId),
      notifyRoleId: this.form.value.notifyRoleId === null || this.form.value.notifyRoleId === ''
        ? null
        : Number(this.form.value.notifyRoleId),
      repeatIntervalHours: this.form.value.repeatIntervalHours === null || this.form.value.repeatIntervalHours === ''
        ? null
        : Number(this.form.value.repeatIntervalHours),
      maxEscalations: this.form.value.maxEscalations === null || this.form.value.maxEscalations === ''
        ? null
        : Number(this.form.value.maxEscalations),
    };

    this.dialogRef.close(payload);
  }
}
