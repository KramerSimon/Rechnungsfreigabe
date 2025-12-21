import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';
import { DashboardService, SystemStatus } from '../../../../core/services/dashboard.service';
// Zentrale Modelle
import {
  AutoApprovalRule,
  AssignmentRule,
  MasterDataSection
} from '../../../../core/models/dashboard.models';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss'],
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatListModule,
    MatDividerModule,
    MatBadgeModule
  ]
})
export class AdminDashboardComponent implements OnInit {
  loading = false;
  systemStatus?: SystemStatus;

  masterDataSections: MasterDataSection[] = [
    {
      id: 'suppliers',
      title: 'Lieferanten-Verwaltung',
      description: 'Stammdaten und Kategorien',
      icon: 'business',
      route: '/admin/suppliers'
    },
    {
      id: 'costcenters',
      title: 'Kostenstellen',
      description: 'Auswahllisten pflegen',
      icon: 'account_tree',
      route: '/admin/cost-centers'
    },
    {
      id: 'projects',
      title: 'Projekte',
      description: 'Auswahllisten pflegen',
      icon: 'account_tree',
      route: '/admin/projects'
    },
    {
      id: 'invoices',
      title: 'Rechnungen',
      description: 'Alle Rechnungen verwalten',
      icon: 'receipt',
      route: '/admin/invoices'
    },
    {
      id: 'users',
      title: 'Benutzer & Rollen',
      description: 'Wer darf freigeben?',
      icon: 'group',
      route: '/admin/users'
    },
    {
      id: 'escalation',
      title: 'Eskalations-Einstellungen',
      description: 'Wann gehen E-Mails raus?',
      icon: 'schedule',
      route: '/admin/escalation'
    },
  ];

  constructor(
    private router: Router,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    this.loadSystemStatus();
  }

  private loadSystemStatus(): void {
    this.loading = true;

    this.dashboardService.getSystemStatus().subscribe({
      next: (status) => {
        this.systemStatus = status;
        this.loading = false;
      },
      error: (error) => {
        console.error('Fehler beim Laden des System-Status:', error);
        // Fallback Daten
        this.systemStatus = {
          servicesActive: true,
          autoApprovalRate: 0,
          lastUpdate: new Date().toISOString(),
          totalInvoicesThisMonth: 0,
          averageProcessingTime: 0
        };
        this.loading = false;
      }
    });
  }

  onManageAutoRules(): void {
    this.router.navigate(['/admin/rules']);
  }

  onManageAssignmentRules(): void {
    this.router.navigate(['/admin/rules']);
  }

  onNavigateToSection(section: MasterDataSection): void {
    this.router.navigate([section.route]);
  }

  toggleAutoRule(rule: AutoApprovalRule): void {
    rule.isActive = !rule.isActive;
    // Hier würde ein API-Call zum Speichern der Änderung gemacht
    console.log(`Auto-Regel ${rule.name} ${rule.isActive ? 'aktiviert' : 'deaktiviert'}`);
  }

  toggleAssignmentRule(rule: AssignmentRule): void {
    rule.isActive = !rule.isActive;
    // Hier würde ein API-Call zum Speichern der Änderung gemacht
    console.log(`Zuweisungs-Regel für ${rule.costCenter} ${rule.isActive ? 'aktiviert' : 'deaktiviert'}`);
  }

  getStatusIcon(): string {
    return this.systemStatus?.servicesActive ? 'check_circle' : 'error';
  }

  getStatusColor(): string {
    return this.systemStatus?.servicesActive ? 'green' : 'red';
  }

  getStatusText(): string {
    return this.systemStatus?.servicesActive ? 'Alle Dienste aktiv' : 'Dienste gestört';
  }

  getAutoApprovalText(): string {
    const rate = this.systemStatus?.autoApprovalRate ?? 0;
    return `Auto-Quote: ${rate}% aller Rechnungen`;
  }

  formatLastUpdate(): string {
    const lastUpdate = this.systemStatus?.lastUpdate ?? new Date().toISOString();
    return new Date(lastUpdate).toLocaleString('de-DE');
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount);
  }
}
