import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from './core/services/auth.service';
import { RoleService, UserRole, DashboardRoute } from './core/services/role.service';
import { AuthState } from './core/models/auth.models';
import { Observable, map } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule
],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Rechnungsfreigabe';
  authState$: Observable<AuthState>;
  availableRoutes$: Observable<DashboardRoute[]>;
  currentRole$: Observable<string>;
  currentRoleTitle$: Observable<string>;

  constructor(
    private authService: AuthService,
    private roleService: RoleService,
    private router: Router
  ) {
    this.authState$ = this.authService.authState$;
    this.availableRoutes$ = this.roleService.getAvailableNavigationRoutes();
    this.currentRole$ = this.roleService.getCurrentUserRole().pipe(
      map(role => role.toString())
    );
    this.currentRoleTitle$ = this.roleService.getCurrentDashboardTitle();
  }

  ngOnInit(): void {
    // Navigation will be handled by auth guard and routing
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
