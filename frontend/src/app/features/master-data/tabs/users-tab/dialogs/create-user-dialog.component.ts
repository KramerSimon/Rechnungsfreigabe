import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { RoleDto, User } from '../../../../../core/models/user.models';

interface CreateUserDialogData {
  user: User;
  availableRoles: RoleDto[];
}

@Component({
  selector: 'app-create-user-dialog',
  templateUrl: './create-user-dialog.component.html',
  styleUrls: ['./create-user-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    ReactiveFormsModule
  ]
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
