import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { RoleService, UserRole } from '../../core/services/role.service';
import { UserDashboardComponent } from './user-dashboard/user-dashboard.component';
import { AccountingDashboardComponent } from './accounting-dashboard/accounting-dashboard.component';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard.component';
@Component({
  selector: 'app-smart-dashboard',
  template: `
    <div class="smart-dashboard-container">
      <!-- User Dashboard -->
      <app-user-dashboard
        *ngIf="(currentRole$ | async) === userRoleEnum.USER">
      </app-user-dashboard>

      <!-- Accounting Dashboard -->
      <app-accounting-dashboard
        *ngIf="(currentRole$ | async) === userRoleEnum.ACCOUNTING">
      </app-accounting-dashboard>

      <!-- Admin Dashboard -->
      <app-admin-dashboard
        *ngIf="(currentRole$ | async) === userRoleEnum.ADMIN">
      </app-admin-dashboard>

      <!-- Loading State -->
      <div class="loading-container" *ngIf="!(currentRole$ | async)">
        <div class="loading-content">
          <div class="spinner"></div>
          <p>Lade Dashboard...</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .smart-dashboard-container {
      width: 100%;
      height: 100%;
    }

    .loading-container {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100vh;
      background: #f8f9fa;
    }

    .loading-content {
      text-align: center;
      color: #666;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid #f0f0f0;
      border-left: 3px solid #1976d2;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 1rem auto;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    p {
      margin: 0;
      font-size: 0.9rem;
    }
  `],
  imports: [
    CommonModule,
    UserDashboardComponent,
    AccountingDashboardComponent,
    AdminDashboardComponent
  ]
})
export class SmartDashboardComponent implements OnInit {
  currentRole$: Observable<UserRole>;

  // Expose enum to template
  userRoleEnum = UserRole;

  constructor(private roleService: RoleService) {
    this.currentRole$ = this.roleService.getCurrentUserRole();
  }

  ngOnInit(): void {
    // Zusätzliche Initialisierungslogik falls nötig
    this.currentRole$.subscribe(role => {
      console.log(`Dashboard wird geladen für Rolle: ${role}`);
    });
  }
}
