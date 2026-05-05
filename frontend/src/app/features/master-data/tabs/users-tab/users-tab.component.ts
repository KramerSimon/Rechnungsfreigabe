import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { User } from '../../../../core/models/user.models';
import { RoleDto } from '../../../../core/models/user.models';
import { UserService } from '../../../../core/services/user.service';
import { RoleService } from '../../../../core/services/role.service';
import { CreateUserDialogComponent } from './dialogs/create-user-dialog.component';
import { EditUserDialogComponent } from './dialogs/edit-user-dialog/edit-user-dialog.component';
import { forkJoin } from 'rxjs';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-users-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatTooltipModule,
  ],
  templateUrl: './users-tab.component.html',
  styleUrls: ['./users-tab.component.scss'],
})
export class UsersTabComponent implements OnInit {
  users: User[] = [];
  roles: RoleDto[] = [];
  loadingUsers = false;
  userColumns = ['username', 'fullName', 'email', 'role', 'isActive', 'lastLogin', 'actions'];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private userService: UserService,
    private roleService: RoleService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loadingUsers = true;
    forkJoin({
      users: this.userService.getUsers(),
      roles: this.roleService.getRoles()
    }).subscribe({
      next: (result) => {
        this.users = result.users;
        this.roles = result.roles;
        this.loadingUsers = false;
      },
      error: (error: any) => {
        console.error('Error loading users:', error);
        this.loadingUsers = false;
        this.snackBar.open(this.t('md.users.error.load'), this.t('common.close'), {
          duration: 3000,
        });
      },
    });
  }

  createUser(): void {
    const dialogData = {
      user: {
        id: '',
        username: '',
        email: '',
        firstName: '',
        lastName: '',
        role: '',
        isActive: true,
      } as User,
      availableRoles: this.roles
    };

    const dialogRef = this.dialog.open(CreateUserDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.userService.addUser(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.users.success.created'), this.t('common.close'), {
              duration: 3000,
            });
            this.loadUsers();
          },
          error: (error: any) => {
            console.error('Error creating user:', error);
            this.snackBar.open(
              this.t('md.users.error.create'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editUser(user: User): void {
    const dialogData = {
      user: user,
      availableRoles: this.roles
    };

    const dialogRef = this.dialog.open(EditUserDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.userService.updateUser(user.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              this.t('md.users.success.updated'),
              this.t('common.close'),
              { duration: 3000 }
            );
            this.loadUsers();
          },
          error: (error: any) => {
            console.error('Error updating user:', error);
            this.snackBar.open(
              this.t('md.users.error.update'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  deleteUser(id: string): void {
    if (confirm(this.t('md.users.confirm.delete'))) {
      this.userService.deleteUser(id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.users.success.deleted'), this.t('common.close'), {
            duration: 3000,
          });
          this.loadUsers();
        },
        error: (error: any) => {
          console.error('Error deleting user:', error);
          this.snackBar.open(
            this.t('md.users.error.delete'),
            this.t('common.close'),
            { duration: 3000 }
          );
        },
      });
    }
  }

  getFullName(user: User): string {
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || '-';
  }

  formatDateTime(date: string | Date): string {
    if (!date) return '-';
    const dateObj = new Date(date);
    return dateObj.toLocaleString('de-DE', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
