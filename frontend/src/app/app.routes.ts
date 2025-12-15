import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { SmartDashboardComponent } from './features/dashboard/smart-dashboard.component';
import { UserDashboardComponent } from './features/dashboard/user-dashboard/user-dashboard.component';
import { AccountingDashboardComponent } from './features/dashboard/accounting-dashboard/accounting-dashboard.component';
import { AdminDashboardComponent } from './features/dashboard/admin-dashboard/admin-dashboard.component';
import { InvoiceDetailComponent } from './features/invoice-detail/invoice-detail.component';
import { AdminRulesComponent } from './features/admin-rules/admin-rules.component';
import { LoginComponent } from './features/login/login.component';
import { MasterDataComponent } from './features/master-data/master-data.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },

  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },

  { path: 'dashboard/smart', component: SmartDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/user', component: UserDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/accounting', component: AccountingDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/admin', component: AdminDashboardComponent, canActivate: [authGuard] },

  { path: 'admin/users', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'users' } },
  { path: 'admin/cost-centers', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'costcenters' } },
  { path: 'admin/suppliers', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'suppliers' } },
  { path: 'admin/escalation', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'escalation' } },

  { path: 'invoice/:id', component: InvoiceDetailComponent },
  { path: 'admin/rules', component: AdminRulesComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/login' }
];
