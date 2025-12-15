import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
// Zentrale Modelle
import { Permission, Role, UserRole } from '../../../core/models/user.models';

@Component({
  selector: 'app-role-management-dialog',
  templateUrl: './role-management-dialog.component.html',
  styleUrls: ['./role-management-dialog.component.scss'],
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatTableModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatTabsModule,
    FormsModule,
    MatIconModule
  ]
})
export class RoleManagementDialogComponent {
  activeTab = 0;

  // Verfügbare Rollen
  availableRoles: Role[] = [
    {
      id: 'USER',
      name: 'Benutzer',
      description: 'Standard-Benutzer mit grundlegenden Rechten',
      permissions: ['invoices.read', 'invoices.approve'],
      isSystemRole: true
    },
    {
      id: 'ACCOUNTING',
      name: 'Buchhaltung',
      description: 'Buchhaltungs-Mitarbeiter mit erweiterten Rechten',
      permissions: ['invoices.read', 'invoices.approve', 'invoices.process', 'reports.read'],
      isSystemRole: true
    },
    {
      id: 'ADMIN',
      name: 'Administrator',
      description: 'Vollzugriff auf alle Funktionen',
      permissions: ['*'],
      isSystemRole: true
    },
    {
      id: 'MANAGER',
      name: 'Manager',
      description: 'Management-Rechte für Genehmigungen',
      permissions: ['invoices.read', 'invoices.approve', 'invoices.reject', 'reports.read', 'users.read'],
      isSystemRole: false
    }
  ];

  // Verfügbare Berechtigungen
  availablePermissions: Permission[] = [
    { id: 'invoices.read', name: 'Rechnungen anzeigen', description: 'Berechtigung zum Anzeigen von Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.create', name: 'Rechnungen erstellen', description: 'Berechtigung zum Erstellen neuer Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.edit', name: 'Rechnungen bearbeiten', description: 'Berechtigung zum Bearbeiten von Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.delete', name: 'Rechnungen löschen', description: 'Berechtigung zum Löschen von Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.approve', name: 'Rechnungen genehmigen', description: 'Berechtigung zum Genehmigen von Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.reject', name: 'Rechnungen ablehnen', description: 'Berechtigung zum Ablehnen von Rechnungen', category: 'Rechnungen' },
    { id: 'invoices.process', name: 'Rechnungen verarbeiten', description: 'Berechtigung zur Verarbeitung von Rechnungen', category: 'Rechnungen' },
    { id: 'users.read', name: 'Benutzer anzeigen', description: 'Berechtigung zum Anzeigen von Benutzern', category: 'Benutzerverwaltung' },
    { id: 'users.create', name: 'Benutzer erstellen', description: 'Berechtigung zum Erstellen neuer Benutzer', category: 'Benutzerverwaltung' },
    { id: 'users.edit', name: 'Benutzer bearbeiten', description: 'Berechtigung zum Bearbeiten von Benutzern', category: 'Benutzerverwaltung' },
    { id: 'users.delete', name: 'Benutzer löschen', description: 'Berechtigung zum Löschen von Benutzern', category: 'Benutzerverwaltung' },
    { id: 'reports.read', name: 'Berichte anzeigen', description: 'Berechtigung zum Anzeigen von Berichten', category: 'Berichte' },
    { id: 'reports.export', name: 'Berichte exportieren', description: 'Berechtigung zum Exportieren von Berichten', category: 'Berichte' },
    { id: 'settings.read', name: 'Einstellungen anzeigen', description: 'Berechtigung zum Anzeigen von Systemeinstellungen', category: 'System' },
    { id: 'settings.edit', name: 'Einstellungen bearbeiten', description: 'Berechtigung zum Bearbeiten von Systemeinstellungen', category: 'System' }
  ];

  // Benutzer mit ihren Rollen (würde normalerweise von der API geladen)
  userRoles: UserRole[] = [
    {
      userId: '1',
      username: 'admin',
      fullName: 'Administrator',
      email: 'admin@company.com',
      roles: ['ADMIN']
    },
    {
      userId: '2',
      username: 'mmüller',
      fullName: 'Max Müller',
      email: 'mmueller@company.com',
      roles: ['USER', 'MANAGER']
    },
    {
      userId: '3',
      username: 'sschmidt',
      fullName: 'Sarah Schmidt',
      email: 'sschmidt@company.com',
      roles: ['ACCOUNTING']
    }
  ];

  // Tabellenspalten
  roleColumns = ['name', 'description', 'permissions', 'isSystemRole', 'actions'];
  userRoleColumns = ['username', 'fullName', 'email', 'roles', 'actions'];
  permissionColumns = ['name', 'description', 'category'];

  constructor(
    public dialogRef: MatDialogRef<RoleManagementDialogComponent>
  ) {}

  onTabChange(index: number): void {
    this.activeTab = index;
  }

  getRoleNames(roleIds: string[]): string {
    return roleIds
      .map(id => this.availableRoles.find(r => r.id === id)?.name)
      .filter(name => name)
      .join(', ');
  }

  getPermissionNames(permissionIds: string[]): string {
    if (permissionIds.includes('*')) {
      return 'Alle Berechtigungen';
    }
    return permissionIds
      .map(id => this.availablePermissions.find(p => p.id === id)?.name)
      .filter(name => name)
      .join(', ');
  }

  editRole(role: Role): void {
    // TODO: Implementiere Rollen-Bearbeitung
    console.log('Edit role:', role);
  }

  deleteRole(role: Role): void {
    if (!role.isSystemRole && confirm(`Möchten Sie die Rolle "${role.name}" wirklich löschen?`)) {
      // TODO: Implementiere Rollen-Löschung
      console.log('Delete role:', role);
    }
  }

  createRole(): void {
    // TODO: Implementiere Rollen-Erstellung
    console.log('Create new role');
  }

  editUserRoles(userRole: UserRole): void {
    // TODO: Implementiere Benutzer-Rollen-Bearbeitung
    console.log('Edit user roles:', userRole);
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    // TODO: Implementiere Speichern der Änderungen
    this.dialogRef.close(true);
  }

  getPermissionCategories(): string[] {
    const categories = [...new Set(this.availablePermissions.map(p => p.category))];
    return categories.sort();
  }

  getPermissionsByCategory(category: string): Permission[] {
    return this.availablePermissions.filter(p => p.category === category);
  }

  getRoleName(roleId: string): string {
    const role = this.availableRoles.find(r => r.id === roleId);
    return role ? role.name : roleId;
  }
}
