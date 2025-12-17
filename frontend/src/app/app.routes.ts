import { Routes } from '@angular/router';
import { DashboardComponent } from './features/smart-dashboard/dashboard/dashboard.component';
import { SmartDashboardComponent } from './features/smart-dashboard/smart-dashboard.component';
import { UserDashboardComponent } from './features/smart-dashboard/dashboard/user-dashboard/user-dashboard.component';
import { AccountingDashboardComponent } from './features/smart-dashboard/dashboard/accounting-dashboard/accounting-dashboard.component';
import { AdminDashboardComponent } from './features/smart-dashboard/dashboard/admin-dashboard/admin-dashboard.component';
import { InvoiceDetailComponent } from './features/invoice-detail/invoice-detail.component';
import { RuleDashboardComponent } from './features/smart-dashboard/dashboard/rule-dashboard/rule-dashboard.component';
import { LoginComponent } from './features/login/login.component';
import { MasterDataComponent } from './features/master-data/master-data.component';
import { authGuard } from './core/guards/auth.guard';
import { PdfUploadDashboardComponent } from './features/smart-dashboard/dashboard/pdf-upload-dashboard/pdf-upload-dashboard.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },

  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },

  { path: 'dashboard/smart', component: SmartDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/user', component: UserDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/accounting', component: AccountingDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/admin', component: AdminDashboardComponent, canActivate: [authGuard] },
  { path: 'dashboard/pdf-upload', component: PdfUploadDashboardComponent, canActivate: [authGuard] },

  { path: 'admin/users', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'users' } },
  { path: 'admin/cost-centers', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'costcenters' } },
  { path: 'admin/suppliers', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'suppliers' } },
  { path: 'admin/invoices', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'invoices' } },
  { path: 'admin/escalation', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'escalation' } },
  { path: 'admin/projects', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'projects' } },
  { path: 'admin/approval_workflows', component: MasterDataComponent, canActivate: [authGuard], data: { activeTab: 'approval_workflows' } },

  { path: 'invoice/:id', component: InvoiceDetailComponent, canActivate: [authGuard] },
  { path: 'admin/rules', component: RuleDashboardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/login' }
];
