import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { RoleDto, User } from '../../../core/models/user.models';

interface CreateUserDialogData {
  user: User;
  availableRoles: RoleDto[];
}

@Component({
  selector: 'app-create-user-dialog',
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
      <h2 mat-dialog-title>Neuer Benutzer</h2>

      <form [formGroup]="userForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Benutzername *</mat-label>
            <input matInput formControlName="username" placeholder="max.mustermann">
            <mat-error *ngIf="userForm.get('username')?.hasError('required')">
              Benutzername ist erforderlich
            </mat-error>
            <mat-error *ngIf="userForm.get('username')?.hasError('maxlength')">
              Benutzername darf maximal 50 Zeichen haben
            </mat-error>
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
              <mat-select-trigger>
                <span class="tag-list">
                  <span
                    *ngFor="let roleId of userForm.get('roleIds')?.value"
                    class="role-tag"
                    [style.backgroundColor]="getRoleById(roleId)?.color || '#ff9800'">
                    {{getRoleById(roleId)?.name || roleId}}
                  </span>
                </span>
              </mat-select-trigger>
              <mat-option *ngFor="let role of availableRoles" [value]="role.id">
                <span class="role-tag" [style.backgroundColor]="role.color || '#ff9800'">
                  {{role.name}}
                </span>
              </mat-option>
            </mat-select>
            <mat-hint>Wählen Sie eine oder mehrere Rollen aus</mat-hint>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Passwort *</mat-label>
            <input matInput type="password" formControlName="password" placeholder="••••••••">
            <mat-error *ngIf="userForm.get('password')?.hasError('required')">
              Passwort ist erforderlich
            </mat-error>
            <mat-error *ngIf="userForm.get('password')?.hasError('minlength')">
              Passwort muss mindestens 8 Zeichen lang sein
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Passwort wiederholen *</mat-label>
            <input matInput type="password" formControlName="passwordConfirm" placeholder="••••••••">
            <mat-error *ngIf="userForm.get('passwordConfirm')?.hasError('required')">
              Passwortbestätigung ist erforderlich
            </mat-error>
            <mat-error *ngIf="userForm.get('passwordConfirm')?.hasError('minlength')">
              Passwort muss mindestens 8 Zeichen lang sein
            </mat-error>
            <mat-error *ngIf="userForm.get('passwordConfirm')?.touched && userForm.hasError('passwordMismatch')">
              Passwörter stimmen nicht überein
            </mat-error>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                (click)="onSave()"
                [disabled]="userForm.invalid">
          Erstellen
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 500px;
      max-width: 95vw;
      overflow-x: hidden;
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

    /* Role tags in dropdown */
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

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding: 16px 0;
    }
  `]
})
export class CreateUserDialogComponent {
  userForm: FormGroup;
  availableRoles: RoleDto[] = [];

  constructor(
    private dialogRef: MatDialogRef<CreateUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateUserDialogData,
    private fb: FormBuilder
  ) {
    this.availableRoles = data?.availableRoles || [];
    const user = data?.user || {} as User;
    this.userForm = this.fb.group({
      username: [user.username || '', [Validators.required, Validators.maxLength(50)]],
      email: [user.email || '', [Validators.required, Validators.email, Validators.maxLength(255)]],
      firstName: [user.firstName || '', [Validators.required, Validators.maxLength(100)]],
      lastName: [user.lastName || '', [Validators.required, Validators.maxLength(100)]],
      roleIds: [(user as any).roleIds || []],
      password: ['', [Validators.required, Validators.minLength(8)]],
      passwordConfirm: ['', [Validators.required, Validators.minLength(8)]]
    }, { validators: this.passwordMatchValidator });

    // Trigger validation when password fields change
    this.userForm.get('password')?.valueChanges.subscribe(() => {
      this.userForm.get('passwordConfirm')?.updateValueAndValidity({ emitEvent: false });
    });

    this.userForm.get('passwordConfirm')?.valueChanges.subscribe(() => {
      this.userForm.updateValueAndValidity({ emitEvent: false });
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const passwordConfirm = control.get('passwordConfirm');

    if (!password || !passwordConfirm) {
      return null;
    }

    if (password.value !== passwordConfirm.value) {
      return { passwordMismatch: true };
    }

    return null;
  }

  getRoleById(roleId: number | string): RoleDto | undefined {
    return this.availableRoles.find(role => String(role.id) === String(roleId));
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.userForm.valid) {
      const formValue = { ...this.userForm.value };
      delete formValue.passwordConfirm; // Bestätigung nicht speichern
      this.dialogRef.close(formValue);
    }
  }
}
