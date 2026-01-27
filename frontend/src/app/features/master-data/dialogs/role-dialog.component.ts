import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { RoleDto } from '../../../core/models/user.models';
import { PermissionsApiService, PermissionDto } from '../../../core/services/permissions-api.service';

interface RoleDialogData {
  mode: 'create' | 'edit';
  role?: RoleDto;
}

@Component({
  selector: 'app-role-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatSelectModule,
    MatOptionModule,
    ReactiveFormsModule,
    MatIconModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Neue Rolle' : 'Rolle bearbeiten' }}</h2>

      <form [formGroup]="form" mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Name *</mat-label>
          <input matInput formControlName="name" [readonly]="data.role?.isSystemRole" />
          <mat-error *ngIf="form.get('name')?.hasError('required')">Name ist erforderlich</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Beschreibung</mat-label>
          <textarea matInput rows="2" formControlName="description"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Berechtigungen</mat-label>
          <mat-select formControlName="permissionIds" multiple>
            <mat-option *ngFor="let perm of availablePermissions" [value]="perm.id">
              {{perm.name}} ({{perm.code}})
            </mat-option>
          </mat-select>
          <mat-hint>Wählen Sie eine oder mehrere Berechtigungen aus</mat-hint>
        </mat-form-field>

        <div class="color-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Farbe</mat-label>
            <input matInput type="color" formControlName="color" />
          </mat-form-field>
          <div class="color-preview" [style.backgroundColor]="form.value.color"></div>
        </div>

        <mat-checkbox formControlName="isSystemRole" [disabled]="data.mode === 'edit' && (data.role?.isSystemRole ?? false)">Systemrolle</mat-checkbox>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary" (click)="onSave()" [disabled]="form.invalid">
          {{ data.mode === 'create' ? 'Erstellen' : 'Speichern' }}
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .dialog-container { width: 520px; max-width: 95vw; }
      .full-width { width: 100%; }
      .dialog-actions { display: flex; justify-content: flex-end; gap: 8px; padding: 16px 0; }
      .color-row { display: flex; align-items: center; gap: 12px; }
      .color-preview { width: 36px; height: 36px; border-radius: 8px; border: 1px solid rgba(0,0,0,0.12); }
    `
  ]
})
export class RoleDialogComponent implements OnInit {
  form: FormGroup;
  availablePermissions: PermissionDto[] = [];
  loadingPermissions = false;

  constructor(
    private dialogRef: MatDialogRef<RoleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RoleDialogData,
    private fb: FormBuilder,
    private permissionsApi: PermissionsApiService
  ) {
    const role = data.role;
    const permIds = (role?.permissions || []).map(p => typeof p === 'string' ? parseInt(p, 10) : p);
    this.form = this.fb.group({
      name: [{ value: role?.name || '', disabled: role?.isSystemRole }, [Validators.required, Validators.maxLength(100)]],
      description: [role?.description || '', [Validators.maxLength(255)]],
      permissionIds: [permIds],
      color: [role?.color || '#ff9800'],
      isSystemRole: [typeof role?.isSystemRole === 'boolean' ? role.isSystemRole : false]
    });
  }

  ngOnInit(): void {
    this.loadPermissions();
  }

  loadPermissions(): void {
    this.loadingPermissions = true;
    this.permissionsApi.getPermissions().subscribe({
      next: (perms) => {
        this.availablePermissions = perms;
        this.loadingPermissions = false;
      },
      error: () => {
        this.loadingPermissions = false;
      }
    });
  }

  private buildPayload() {
    const raw = this.form.getRawValue();

    return {
      name: raw.name,
      description: raw.description?.trim() || undefined,
      permissions: raw.permissionIds || [],
      color: raw.color || undefined,
      isSystemRole: raw.isSystemRole
    };
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.buildPayload());
    }
  }
}
