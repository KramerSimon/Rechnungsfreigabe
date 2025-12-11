import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { SmartDashboardComponent } from './dashboard/smart-dashboard.component';
import { UserDashboardComponent } from './dashboard/user-dashboard/user-dashboard.component';
import { AccountingDashboardComponent } from './dashboard/accounting-dashboard/accounting-dashboard.component';
import { AdminDashboardComponent } from './dashboard/admin-dashboard/admin-dashboard.component';
import { InvoiceDetailComponent } from './invoice-detail/invoice-detail.component';
import { AdminRulesComponent } from './admin-rules/admin-rules.component';
import { LoginComponent } from './features/login/login.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },

  // Legacy Dashboard - Redirects to role-based dashboard
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },

  // Role-based Dashboards
  { path: 'dashboard/smart', component: SmartDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/user', component: UserDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/accounting', component: AccountingDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/admin', component: AdminDashboardComponent, canActivate: [authGuard] },

  { path: 'invoice/:id', component: InvoiceDetailComponent }, // Temporarily removed auth guard
  { path: 'admin/rules', component: AdminRulesComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/login' }
];
