import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService, UserRole } from '../../core/services/role.service';
import { Observable } from 'rxjs';
import { UserDashboardComponent } from './dashboard/user-dashboard/user-dashboard.component';
import { AccountingDashboardComponent } from './dashboard/accounting-dashboard/accounting-dashboard.component';
import { AdminDashboardComponent } from './dashboard/admin-dashboard/admin-dashboard.component';
import { PdfUploadDashboardComponent } from './dashboard/pdf-upload-dashboard/pdf-upload-dashboard.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-smart-dashboard',
  imports: [
    UserDashboardComponent,
    AccountingDashboardComponent,
    AdminDashboardComponent,
    PdfUploadDashboardComponent,
    CommonModule,
  ],
  templateUrl: './smart-dashboard.component.html',
  styleUrl: './smart-dashboard.component.scss',
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
    this.currentRole$.subscribe((role) => {
      console.log(`Dashboard wird geladen für Rolle: ${role}`);
    });
  }
}
