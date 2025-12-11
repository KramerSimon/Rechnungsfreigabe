import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InvoiceDetailComponent } from './invoice-detail/invoice-detail.component';
import { AdminRulesComponent } from './admin-rules/admin-rules.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'invoice/:id', component: InvoiceDetailComponent },
  { path: 'admin/rules', component: AdminRulesComponent }
];
