import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const permissionGuard = (route: any) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredPermissions: string[] = route?.data?.requiredPermissions || [];

  return authService.authState$.pipe(
    take(1),
    map(authState => {
      if (!authState.isAuthenticated) {
        router.navigate(['/login']);
        return false;
      }

      const permissions = authState.permissions || [];

      const hasAnyDashboardPermission = permissions.some(p => p.startsWith('dashboards.view_')) ||
        permissions.includes('dashboards.view_all');

      if (permissions.includes('dashboards.view_all')) {
        return true;
      }

      if (!requiredPermissions.length) {
        return true;
      }

      const hasPermission = requiredPermissions.some(p => permissions.includes(p));
      if (!hasPermission) {
        router.navigate([hasAnyDashboardPermission ? '/dashboard' : '/login']);
      }
      return hasPermission;
    })
  );
};
