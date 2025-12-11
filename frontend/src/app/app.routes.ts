import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InvoiceDetailComponent } from './invoice-detail/invoice-detail.component';
import { AdminRulesComponent } from './admin-rules/admin-rules.component';
import { LoginComponent } from './features/login/login.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'invoice/:id', component: InvoiceDetailComponent, canActivate: [authGuard] },
  { path: 'admin/rules', component: AdminRulesComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/login' }
];
