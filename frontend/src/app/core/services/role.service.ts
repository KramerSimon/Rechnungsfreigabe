import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { User } from '../models/auth.models';
import { DashboardRoute } from '../models';
import { map, Observable } from 'rxjs';

// Re-export for backward compatibility
export type { DashboardRoute };

export enum UserRole {
  ADMIN = 'admin',
  ACCOUNTING = 'accounting',
  USER = 'user',
  PDF_UPLOADER = 'pdf_uploader'
}

@Injectable({
  providedIn: 'root'
})
export class RoleService {

  private dashboardRoutes: DashboardRoute[] = [
    {
      path: '/dashboard/user',
      component: 'UserDashboard',
      role: UserRole.USER,
      title: 'Meine Aufgaben'
    },
    {
      path: '/dashboard/accounting',
      component: 'AccountingDashboard',
      role: UserRole.ACCOUNTING,
      title: 'Cockpit'
    },
    {
      path: '/dashboard/admin',
      component: 'AdminDashboard',
      role: UserRole.ADMIN,
      title: 'Konfiguration'
    },
    {
      path: '/dashboard/pdf-upload',
      component: 'PdfUploadDashboard',
      role: UserRole.PDF_UPLOADER,
      title: 'PDF Upload'
    },
    {
      path: '/dashboard/rules',
      component: 'RuleDashboard',
      role: UserRole.ADMIN,
      title: 'Regeln'
    }
  ];

  constructor(private authService: AuthService) {}

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
   * Gibt die Dashboard-Route für eine bestimmte Rolle zurück
   */
  getDashboardRouteForRole(role: UserRole): string {
    const route = this.dashboardRoutes.find(r => r.role === role);
    return route?.path || '/dashboard/user'; // Fallback zu User-Dashboard
  }

  /**
   * Gibt die Dashboard-Route für den aktuellen Benutzer zurück
   */
  getCurrentUserDashboardRoute(): Observable<string> {
    return this.getCurrentUserRole().pipe(
      map(role => this.getDashboardRouteForRole(role))
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
   * Gibt die Navigations-Optionen für den aktuellen Benutzer zurück
   */
  getAvailableNavigationRoutes(): Observable<DashboardRoute[]> {
    return this.getCurrentUserRole().pipe(
      map(userRole => {
        switch (userRole) {
          case UserRole.ADMIN:
            // Admin sieht alle Dashboards
            return this.dashboardRoutes;

          case UserRole.ACCOUNTING:
            // Buchhaltung sieht Accounting und User
            return this.dashboardRoutes.filter(route =>
              route.role === UserRole.ACCOUNTING || route.role === UserRole.USER
            );

          case UserRole.USER:
          default:
            // User sieht nur User-Dashboard
            return this.dashboardRoutes.filter(route => route.role === UserRole.USER);
        }
      })
    );
  }

  /**
   * Gibt den Dashboard-Titel für eine Rolle zurück
   */
  getDashboardTitleForRole(role: UserRole): string {
    const route = this.dashboardRoutes.find(r => r.role === role);
    return route?.title || 'Dashboard';
  }

  /**
   * Gibt den aktuellen Dashboard-Titel zurück
   */
  getCurrentDashboardTitle(): Observable<string> {
    return this.getCurrentUserRole().pipe(
      map(role => this.getDashboardTitleForRole(role))
    );
  }
}
