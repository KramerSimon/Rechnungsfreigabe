import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CreateEscalationRuleDto, EscalationRule, StatusDto } from '../../../../../core/models/escalation-rule.model';
import { RoleDto, User } from '../../../../../core/models/user.models';
import { StatusDisplayPipe } from '../../../../../core/pipes/status-display.pipe';
import { LanguageService } from '../../../../../core/services/language.service';

export interface EscalationRuleDialogData {
  mode: 'create' | 'edit';
  statuses: StatusDto[];
  roles: RoleDto[];
  users: User[];
  rule?: EscalationRule;
}

@Component({
  selector: 'app-escalation-rule-dialog',
  templateUrl: './escalation-rule-dialog.component.html',
  styleUrls: ['./escalation-rule-dialog.component.scss'],
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
    StatusDisplayPipe,
  ]
})
export class EscalationRuleDialogComponent {
  form: FormGroup;
  dQ = '{'; // for displaying curly braces in template hints

  constructor(
    private dialogRef: MatDialogRef<EscalationRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EscalationRuleDialogData,
    private fb: FormBuilder,
    private languageService: LanguageService,
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

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}

