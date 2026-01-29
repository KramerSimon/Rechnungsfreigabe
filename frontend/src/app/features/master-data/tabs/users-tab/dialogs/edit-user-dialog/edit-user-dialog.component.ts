import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RoleDto, User } from '../../../../../../core/models/user.models';

interface EditUserDialogData {
  user: User;
  availableRoles: RoleDto[];
}

@Component({
  selector: 'app-edit-user-dialog',
  templateUrl: './edit-user-dialog.component.html',
  styleUrls: ['./edit-user-dialog.component.scss'],
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
export class EditUserDialogComponent {
  userForm: FormGroup;
  availableRoles: RoleDto[] = [];

  constructor(
    private dialogRef: MatDialogRef<EditUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EditUserDialogData,
    private fb: FormBuilder
  ) {
    this.availableRoles = data?.availableRoles || [];
    const user = data?.user || {} as User;
    const roleIds = this.getRoleIds(user.roles);

    this.userForm = this.fb.group({
      username: [{ value: user.username || '', disabled: true }], // Username is readonly
      email: [user.email || '', [Validators.required, Validators.email, Validators.maxLength(255)]],
      firstName: [user.firstName || '', [Validators.required, Validators.maxLength(100)]],
      lastName: [user.lastName || '', [Validators.required, Validators.maxLength(100)]],
      activeDirectorySid: ['', [Validators.maxLength(255)]],
      roleIds: [roleIds, []],
      isActive: [user.isActive]
    });
  }

  private getRoleIds(roles?: Array<{ id: number; name: string }>): number[] {
    if (!roles || roles.length === 0) {
      return [];
    }
    return roles.map(role => role.id);
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
      delete formValue.username;
      this.dialogRef.close(formValue);
    }
  }
}
