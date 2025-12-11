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

interface SystemStatus {
  servicesActive: boolean;
  autoApprovalRate: number;
  lastUpdate: string;
}

interface AutoApprovalRule {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  condition: string;
}

interface AssignmentRule {
  id: number;
  costCenter: string;
  assignedTo: string;
  isActive: boolean;
}

interface MasterDataSection {
  id: string;
  title: string;
  description: string;
  icon: string;
  count?: number;
  route: string;
}

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

  systemStatus: SystemStatus = {
    servicesActive: true,
    autoApprovalRate: 35,
    lastUpdate: new Date().toISOString()
  };

  autoApprovalRules: AutoApprovalRule[] = [
    {
      id: 1,
      name: 'Kleinbeträge',
      description: 'Betrag < 50€',
      isActive: true,
      condition: 'amount < 50'
    },
    {
      id: 2,
      name: 'Bekannte Lieferanten',
      description: 'Lieferant "Büro"',
      isActive: true,
      condition: 'supplier.category = "office"'
    },
    {
      id: 3,
      name: 'Wiederkehrende Rechnungen',
      description: 'Monatliche Abos',
      isActive: true,
      condition: 'isRecurring = true'
    }
  ];

  assignmentRules: AssignmentRule[] = [
    {
      id: 1,
      costCenter: 'KST 4020',
      assignedTo: 'M. Müller (IT)',
      isActive: true
    },
    {
      id: 2,
      costCenter: 'KST 1010',
      assignedTo: 'S. Schmidt (Marketing)',
      isActive: true
    },
    {
      id: 3,
      costCenter: 'KST 2030',
      assignedTo: 'A. Weber (Einkauf)',
      isActive: true
    }
  ];

  masterDataSections: MasterDataSection[] = [
    {
      id: 'users',
      title: 'Benutzer & Rollen',
      description: 'Wer darf freigeben?',
      icon: 'group',
      count: 24,
      route: '/admin/users'
    },
    {
      id: 'costcenters',
      title: 'Kostenstellen & Projekte',
      description: 'Auswahllisten pflegen',
      icon: 'account_tree',
      count: 15,
      route: '/admin/cost-centers'
    },
    {
      id: 'escalation',
      title: 'Eskalations-Einstellungen',
      description: 'Wann gehen E-Mails raus?',
      icon: 'schedule',
      route: '/admin/escalation'
    },
    {
      id: 'suppliers',
      title: 'Lieferanten-Verwaltung',
      description: 'Stammdaten und Kategorien',
      icon: 'business',
      count: 87,
      route: '/admin/suppliers'
    }
  ];

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadSystemStatus();
  }

  private loadSystemStatus(): void {
    this.loading = true;

    // Simuliere System-Status laden
    setTimeout(() => {
      // In Realität würde hier ein API-Call gemacht werden
      this.loading = false;
    }, 500);
  }

  onManageAutoRules(): void {
    this.router.navigate(['/admin/auto-approval-rules']);
  }

  onManageAssignmentRules(): void {
    this.router.navigate(['/admin/assignment-rules']);
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
    return this.systemStatus.servicesActive ? 'check_circle' : 'error';
  }

  getStatusColor(): string {
    return this.systemStatus.servicesActive ? 'green' : 'red';
  }

  getStatusText(): string {
    return this.systemStatus.servicesActive ? 'Alle Dienste aktiv' : 'Dienste gestört';
  }

  getAutoApprovalText(): string {
    return `Auto-Quote: ${this.systemStatus.autoApprovalRate}% aller Rechnungen`;
  }

  formatLastUpdate(): string {
    return new Date(this.systemStatus.lastUpdate).toLocaleString('de-DE');
  }
}
