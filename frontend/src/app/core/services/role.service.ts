import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { User } from '../models/auth.models';
import { DashboardRoute, RoleDto } from '../models';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// Re-export for backward compatibility
export type { DashboardRoute };

export enum UserRole {
  ADMIN = 'admin',
  ACCOUNTING = 'accounting',
  USER = 'user',
  PDF_UPLOADER = 'pdf_uploader'
}

// Dashboard permission mapping
export interface DashboardConfig {
  [key: string]: {
    path: string;
    component: string;
    requiredPermissions: string[];
    title: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class RoleService {

  private readonly baseUrl = `${environment.apiUrl}/v1/roles`;

  private dashboardConfig: DashboardConfig = {
    user: {
      path: '/dashboard/user',
      component: 'UserDashboard',
      requiredPermissions: ['invoices.view_own', 'invoices.view_all'],
      title: 'Meine Aufgaben'
    },
    accounting: {
      path: '/dashboard/accounting',
      component: 'AccountingDashboard',
      requiredPermissions: ['invoices.view_all', 'invoices.approve'],
      title: 'Cockpit'
    },
    admin: {
      path: '/dashboard/admin',
      component: 'AdminDashboard',
      requiredPermissions: ['users.manage', 'roles.manage', 'permissions.manage'],
      title: 'Konfiguration'
    },
    'pdf-upload': {
      path: '/dashboard/pdf-upload',
      component: 'PdfUploadDashboard',
      requiredPermissions: ['invoices.create'],
      title: 'PDF Upload'
    },
    rules: {
      path: '/dashboard/rules',
      component: 'RuleDashboard',
      requiredPermissions: ['roles.manage'],
      title: 'Regeln'
    }
  };

  constructor(
    private authService: AuthService,
    private http: HttpClient
  ) {}

  /**
   * Ermittelt die Haupt-Rolle des aktuellen Benutzers
   */
  getCurrentUserRole(): Observable<UserRole> {
    return this.authService.authState$.pipe(
      map(authState => {
        if (!authState.user || !authState.user.roles) {
          return UserRole.USER; // Default fallback
        }

        return this.determineHighestRole(authState.user.roles.map(role => role.name));
      })
    );
  }

  /**
   * Bestimmt die höchste Rolle basierend auf Hierarchie
   */
  private determineHighestRole(roleNames: string[]): UserRole {
    const normalizedRoles = roleNames.map(name => name.toLowerCase());

    // Admin hat die höchste Priorität
    if (normalizedRoles.some(role =>
        role.includes('admin') ||
        role.includes('administrator') ||
        role.includes('system')
    )) {
      return UserRole.ADMIN;
    }

    // Buchhaltung ist zweit-höchste Priorität
    if (normalizedRoles.some(role =>
        role.includes('accounting') ||
        role.includes('buchhaltung') ||
        role.includes('finance') ||
        role.includes('finanz')
    )) {
      return UserRole.ACCOUNTING;
    }

    // Standard User
    return UserRole.USER;
  }

  /**
   * Prüft ob der aktuelle Benutzer eine bestimmte Permission hat
   */
  hasPermission(permission: string): Observable<boolean> {
    return this.authService.authState$.pipe(
      map(authState => {
        if (!authState.user || !authState.user.roles) {
          return false;
        }

        // Flatten all permissions from all roles
        const userPermissions = authState.user.roles
          .flatMap(role => role.permissions || [])
          .map(p => typeof p === 'string' ? p : p.toString());

        return userPermissions.includes(permission);
      })
    );
  }

  /**
   * Prüft ob der aktuelle Benutzer mehrere Permissions hat
   */
  hasAllPermissions(permissions: string[]): Observable<boolean> {
    return this.authService.authState$.pipe(
      map(authState => {
        if (!authState.user || !authState.user.roles) {
          return false;
        }

        const userPermissions = authState.user.roles
          .flatMap(role => role.permissions || [])
          .map(p => typeof p === 'string' ? p : p.toString());

        return permissions.every(permission => userPermissions.includes(permission));
      })
    );
  }

  /**
   * Gibt die verfügbaren Dashboard-Routen zurück
   * Alle Benutzer sehen alle Navigation-Buttons
   * Der Zugriff wird durch Route Guards kontrolliert
   */
  getAvailableNavigationRoutes(): Observable<DashboardRoute[]> {
    return this.authService.authState$.pipe(
      map(authState => {
        if (!authState.user || !authState.user.roles) {
          return [];
        }

        // Alle Benutzer sehen alle verfügbaren Dashboard-Routen
        return Object.values(this.dashboardConfig).map(dashboard => ({
          path: dashboard.path,
          component: dashboard.component,
          role: this.getDefaultRoleForPath(dashboard.path),
          title: dashboard.title
        }));
      })
    );
  }

  /**
   * Gibt die Dashboard-Route für den aktuellen Benutzer zurück
   */
  getCurrentUserDashboardRoute(): Observable<string> {
    return this.getAvailableNavigationRoutes().pipe(
      map(routes => {
        // Return first available route, default to user dashboard
        return routes.length > 0 ? routes[0].path : '/dashboard/user';
      })
    );
  }

  /**
   * Prüft ob der aktuelle Benutzer eine bestimmte Rolle hat
   */
  hasRole(requiredRole: UserRole): Observable<boolean> {
    return this.getCurrentUserRole().pipe(
      map(userRole => {
        // Admin hat Zugriff auf alles
        if (userRole === UserRole.ADMIN) {
          return true;
        }

        // Accounting hat Zugriff auf User-Bereich
        if (userRole === UserRole.ACCOUNTING && requiredRole === UserRole.USER) {
          return true;
        }

        // Exakte Rolle erforderlich
        return userRole === requiredRole;
      })
    );
  }

  /**
   * Gibt den Dashboard-Titel für eine Rolle zurück (Fallback)
   */
  getDashboardTitleForRole(role: UserRole): string {
    const config = this.dashboardConfig[this.getRoleKey(role)];
    return config?.title || 'Dashboard';
  }

  /**
   * Gibt den aktuellen Dashboard-Titel zurück
   */
  getCurrentDashboardTitle(): Observable<string> {
    return this.getCurrentUserRole().pipe(
      map(role => this.getDashboardTitleForRole(role))
    );
  }

  /**
   * Hilfsfunktion: Konvertiert UserRole zu Dashboard-Config-Key
   */
  private getRoleKey(role: UserRole): string {
    const mapping: { [key in UserRole]: string } = {
      [UserRole.ADMIN]: 'admin',
      [UserRole.ACCOUNTING]: 'accounting',
      [UserRole.USER]: 'user',
      [UserRole.PDF_UPLOADER]: 'pdf-upload'
    };
    return mapping[role];
  }

  /**
   * Hilfsfunktion: Bestimmt Standard-Rolle für einen Dashboard-Path
   */
  private getDefaultRoleForPath(path: string): UserRole {
    const mapping: { [key: string]: UserRole } = {
      '/dashboard/user': UserRole.USER,
      '/dashboard/accounting': UserRole.ACCOUNTING,
      '/dashboard/admin': UserRole.ADMIN,
      '/dashboard/pdf-upload': UserRole.PDF_UPLOADER,
      '/dashboard/rules': UserRole.ADMIN
    };
    return mapping[path] || UserRole.USER;
  }

  // ========== ROLE MANAGEMENT API METHODS ==========

  /**
   * Ruft alle Rollen ab
   */
  getRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(this.baseUrl);
  }

  /**
   * Erstellt eine neue Rolle
   */
  createRole(payload: CreateRoleRequest): Observable<RoleDto> {
    return this.http.post<RoleDto>(this.baseUrl, payload);
  }

  /**
   * Aktualisiert eine Rolle
   */
  updateRole(id: number, payload: UpdateRoleRequest): Observable<RoleDto> {
    return this.http.put<RoleDto>(`${this.baseUrl}/${id}`, payload);
  }

  /**
   * Löscht eine Rolle
   */
  deleteRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

// API Request/Response types
export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions?: string[];
  color?: string;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissions?: string[];
  color?: string;
}
