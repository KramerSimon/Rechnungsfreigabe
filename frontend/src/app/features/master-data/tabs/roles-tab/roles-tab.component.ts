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
    private snackBar: MatSnackBar
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
        this.snackBar.open('Failed to load roles', 'Close', { duration: 3000 });
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
            this.snackBar.open('Role created successfully', 'Close', { duration: 3000 });
            this.loadRoles();
          },
          error: (error: any) => {
            console.error('Error creating role:', error);
            this.snackBar.open('Failed to create role', 'Close', { duration: 3000 });
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
            this.snackBar.open('Role updated successfully', 'Close', { duration: 3000 });
            this.loadRoles();
          },
          error: (error: any) => {
            console.error('Error updating role:', error);
            this.snackBar.open('Failed to update role', 'Close', { duration: 3000 });
          }
        });
      }
    });
  }

  deleteRole(role: RoleDto): void {
    if (role.isSystemRole) {
      this.snackBar.open('System roles cannot be deleted', 'Close', { duration: 3000 });
      return;
    }

    if (confirm(`Are you sure you want to delete the role "${role.name}"?`)) {
      this.roleService.deleteRole(role.id).subscribe({
        next: () => {
          this.snackBar.open('Role deleted successfully', 'Close', { duration: 3000 });
          this.loadRoles();
        },
        error: (error: any) => {
          console.error('Error deleting role:', error);
          this.snackBar.open('Failed to delete role', 'Close', { duration: 3000 });
        }
      });
    }
  }
}
