import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLogin?: string;
  createdAt: string;
}

interface Role {
  id: number;
  name: string;
}

@Component({
  selector: 'app-edit-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Benutzer bearbeiten</h2>

      <form [formGroup]="userForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Benutzername *</mat-label>
            <input matInput formControlName="username" readonly>
            <mat-hint>Benutzername kann nicht geändert werden</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>E-Mail *</mat-label>
            <input matInput type="email" formControlName="email" placeholder="max@firma.de">
            <mat-error *ngIf="userForm.get('email')?.hasError('required')">
              E-Mail ist erforderlich
            </mat-error>
            <mat-error *ngIf="userForm.get('email')?.hasError('email')">
              Ungültige E-Mail-Adresse
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Vorname *</mat-label>
            <input matInput formControlName="firstName" placeholder="Max">
            <mat-error *ngIf="userForm.get('firstName')?.hasError('required')">
              Vorname ist erforderlich
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Nachname *</mat-label>
            <input matInput formControlName="lastName" placeholder="Mustermann">
            <mat-error *ngIf="userForm.get('lastName')?.hasError('required')">
              Nachname ist erforderlich
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Active Directory SID</mat-label>
            <input matInput formControlName="activeDirectorySid"
                   placeholder="Optional - für AD-Integration">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Rollen</mat-label>
            <mat-select formControlName="roleIds" multiple>
              <mat-option *ngFor="let role of availableRoles" [value]="role.id">
                {{role.name}}
              </mat-option>
            </mat-select>
            <mat-hint>Wählen Sie eine oder mehrere Rollen aus</mat-hint>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="isActive">
              <mat-option [value]="true">Aktiv</mat-option>
              <mat-option [value]="false">Inaktiv</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                (click)="onSave()"
                [disabled]="userForm.invalid">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 500px;
      max-width: 90vw;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 8px;
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
      padding: 16px 0;
    }
  `]
})
export class EditUserDialogComponent {
  userForm: FormGroup;
  availableRoles: Role[] = [
    { id: 1, name: 'Administrator' },
    { id: 2, name: 'Freigeber' },
    { id: 3, name: 'Buchhaltung' },
    { id: 4, name: 'Benutzer' },
    { id: 5, name: 'Manager' }
  ];

  constructor(
    private dialogRef: MatDialogRef<EditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: User,
    private fb: FormBuilder
  ) {
    // Extract role IDs from role name (simplified mapping)
    const roleIds = this.getRoleIdsFromRoleName(data.role);

    this.userForm = this.fb.group({
      username: [{ value: data.username || '', disabled: true }], // Username is readonly
      email: [data.email || '', [Validators.required, Validators.email, Validators.maxLength(255)]],
      firstName: [data.firstName || '', [Validators.required, Validators.maxLength(100)]],
      lastName: [data.lastName || '', [Validators.required, Validators.maxLength(100)]],
      activeDirectorySid: ['', [Validators.maxLength(255)]], // Not available in current User interface
      roleIds: [roleIds, []],
      isActive: [data.isActive]
    });
  }

  private getRoleIdsFromRoleName(roleName: string): number[] {
    // Simple mapping - in real app this would come from API
    const roleMap: { [key: string]: number } = {
      'Administrator': 1,
      'Freigeber': 2,
      'Buchhaltung': 3,
      'Benutzer': 4,
      'Manager': 5
    };

    const roleId = roleMap[roleName];
    return roleId ? [roleId] : [];
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.userForm.valid) {
      // Don't include the disabled username field
      const formValue = { ...this.userForm.value };
      delete formValue.username;
      this.dialogRef.close(formValue);
    }
  }
}
