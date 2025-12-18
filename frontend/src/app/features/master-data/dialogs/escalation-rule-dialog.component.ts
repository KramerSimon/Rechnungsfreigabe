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

export interface EscalationRuleDialogData {
  mode: 'create' | 'edit';
  statuses: string[];
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
            <input matInput formControlName="notifyRole" placeholder="z.B. Manager">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Benutzer-ID (optional)</mat-label>
            <input matInput type="number" formControlName="notifyUserId" min="1">
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
    .dialog-container {
      width: 720px;
      max-width: 95vw;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 12px;
      align-items: flex-start;
    }

    .form-row mat-form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 12px 0;
    }

    textarea {
      resize: vertical;
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
      notifyRole: [rule?.notifyRole || ''],
      notifyUserId: [rule?.notifyUserId ?? null, [Validators.min(1)]],
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
