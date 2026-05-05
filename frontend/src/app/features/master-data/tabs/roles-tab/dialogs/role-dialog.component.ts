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
import { RoleDto } from '../../../../../core/models/user.models';
import { PermissionsApiService, PermissionDto } from '../../../../../core/services/permissions-api.service';
import { LanguageService } from '../../../../../core/services/language.service';

interface RoleDialogData {
  mode: 'create' | 'edit';
  role?: RoleDto;
}

@Component({
  selector: 'app-role-dialog',
  templateUrl: './role-dialog.component.html',
  styleUrls: ['./role-dialog.component.scss'],
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
  ]
})
export class RoleDialogComponent implements OnInit {
  form: FormGroup;
  availablePermissions: PermissionDto[] = [];
  loadingPermissions = false;
  colorOptions: string[] = ['#ff9800', '#f44336', '#e91e63', '#9c27b0', '#673ab7', '#3f51b5', '#2196f3', '#03a9f4', '#00bcd4', '#009688', '#4caf50', '#8bc34a', '#cddc39', '#ffc107', '#ff5722', '#795548', '#607d8b'];

  constructor(
    private dialogRef: MatDialogRef<RoleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RoleDialogData,
    private fb: FormBuilder,
    @Inject(PermissionsApiService) private permissionsApi: PermissionsApiService,
    private languageService: LanguageService
  ) {
    const role = data.role;
    const isSystemRole = !!role?.isSystemRole;
    const permIds = (role?.permissions || []).map((p: any) => typeof p === 'string' ? parseInt(p, 10) : p);
    this.form = this.fb.group({
      name: [{ value: role?.name || '', disabled: isSystemRole }, [Validators.required, Validators.maxLength(100)]],
      description: [{ value: role?.description || '', disabled: isSystemRole }, [Validators.maxLength(255)]],
      permissionIds: [{ value: permIds, disabled: isSystemRole }],
      color: [role?.color || '#ff9800'],
      isSystemRole: [{ value: typeof role?.isSystemRole === 'boolean' ? role.isSystemRole : false, disabled: true }]
    });
  }

  ngOnInit(): void {
    this.loadPermissions();
  }

  loadPermissions(): void {
    this.loadingPermissions = true;
    this.permissionsApi.getPermissions().subscribe({
      next: (perms: PermissionDto[]) => {
        this.availablePermissions = perms;
        this.loadingPermissions = false;
      },
      error: () => {
        this.loadingPermissions = false;
      }
    });
  }

  setColor(color: string): void {
    this.form.patchValue({ color });
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

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
