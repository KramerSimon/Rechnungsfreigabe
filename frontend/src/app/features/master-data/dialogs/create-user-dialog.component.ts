import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { User } from '../../../core';

interface Role {
  id: number;
  name: string;
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
              <mat-option *ngFor="let role of availableRoles" [value]="role.id">
                {{role.name}}
              </mat-option>
            </mat-select>
            <mat-hint>Wählen Sie eine oder mehrere Rollen aus</mat-hint>
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
export class CreateUserDialogComponent {
  userForm: FormGroup;
  availableRoles: Role[] = [
    { id: 1, name: 'Administrator' },
    { id: 2, name: 'Freigeber' },
    { id: 3, name: 'Buchhaltung' },
    { id: 4, name: 'Benutzer' },
    { id: 5, name: 'Manager' }
  ];

  constructor(
    private dialogRef: MatDialogRef<CreateUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: User,
    private fb: FormBuilder
  ) {
    this.userForm = this.fb.group({
      username: [data.username || '', [Validators.required, Validators.maxLength(50)]],
      email: [data.email || '', [Validators.required, Validators.email, Validators.maxLength(255)]],
      firstName: [data.firstName || '', [Validators.required, Validators.maxLength(100)]],
      lastName: [data.lastName || '', [Validators.required, Validators.maxLength(100)]],
      roleIds: [data.roles || [], []]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.userForm.valid) {
      this.dialogRef.close(this.userForm.value);
    }
  }
}
