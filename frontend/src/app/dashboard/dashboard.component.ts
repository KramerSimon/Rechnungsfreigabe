import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../core/services/role.service';

@Component({
  selector: 'app-dashboard',
  template: `
    <div class="dashboard-redirect">
      <div class="loading-content">
        <div class="spinner"></div>
        <p>Weiterleitung zum Dashboard...</p>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-redirect {
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
  `]
})
export class DashboardComponent implements OnInit {

  constructor(
    private router: Router,
    private roleService: RoleService
  ) {}

  ngOnInit(): void {
    // Redirect zum rollenbasierten Dashboard
    this.roleService.getCurrentUserDashboardRoute().subscribe(route => {
      this.router.navigate([route]);
    });
  }
}
