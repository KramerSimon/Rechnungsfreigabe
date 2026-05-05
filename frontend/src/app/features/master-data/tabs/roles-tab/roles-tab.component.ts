import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { RoleService } from '../../../../core/services/role.service';
import { RoleDto } from '../../../../core/models/user.models';
import { RoleDialogComponent } from './dialogs/role-dialog.component';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-roles-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule
  ],
  templateUrl: './roles-tab.component.html',
  styleUrls: ['./roles-tab.component.scss']
})
export class RolesTabComponent implements OnInit {
  displayedColumns: string[] = ['color', 'name', 'description', 'permissions', 'isSystemRole', 'actions'];
  roles: RoleDto[] = [];
  isLoading = false;

  constructor(
    private roleService: RoleService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.isLoading = true;
    this.roleService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading roles:', error);
        this.snackBar.open(this.t('md.roles.error.load'), this.t('common.close'), { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  createRole(): void {
    const dialogRef = this.dialog.open(RoleDialogComponent, {
      width: '600px',
      data: { mode: 'create' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.roleService.createRole(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.roles.success.created'), this.t('common.close'), { duration: 3000 });
            this.loadRoles();
          },
          error: (error: any) => {
            console.error('Error creating role:', error);
            this.snackBar.open(this.t('md.roles.error.create'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  editRole(role: RoleDto): void {
    const dialogRef = this.dialog.open(RoleDialogComponent, {
      width: '600px',
      data: { mode: 'edit', role: { ...role } }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.roleService.updateRole(role.id, result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.roles.success.updated'), this.t('common.close'), { duration: 3000 });
            this.loadRoles();
          },
          error: (error: any) => {
            console.error('Error updating role:', error);
            this.snackBar.open(this.t('md.roles.error.update'), this.t('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }

  deleteRole(role: RoleDto): void {
    if (role.isSystemRole) {
      this.snackBar.open(this.t('md.roles.cannotDeleteSystem'), this.t('common.close'), { duration: 3000 });
      return;
    }

    if (confirm(this.t('md.roles.confirm.delete').replace('{name}', role.name))) {
      this.roleService.deleteRole(role.id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.roles.success.deleted'), this.t('common.close'), { duration: 3000 });
          this.loadRoles();
        },
        error: (error: any) => {
          console.error('Error deleting role:', error);
          this.snackBar.open(this.t('md.roles.error.delete'), this.t('common.close'), { duration: 3000 });
        }
      });
    }
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
