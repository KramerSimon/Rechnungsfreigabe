import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DashboardService, SystemStatus } from '../../../../core/services/dashboard.service';
import { SystemConfigService } from '../../../../core/services/system-config.service';
import {
  AutoApprovalRule,
  AssignmentRule,
  ConfigDataType,
  MasterDataSection,
  SystemConfig
} from '../../../../core/models';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatListModule,
    MatDividerModule,
    MatBadgeModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule
  ]
})
export class AdminDashboardComponent implements OnInit {
  loading = false;
  saving = false;
  systemStatus?: SystemStatus;
  configs: SystemConfig[] = [];
  selectedConfig?: SystemConfig;
  configForm: FormGroup;
  dataTypes: ConfigDataType[] = ['String', 'Number', 'Boolean', 'Json'];

  readonly defaultSystemStatus: SystemStatus = {
    servicesActive: true,
    autoApprovalRate: 0,
    lastUpdate: new Date().toISOString(),
    totalInvoicesThisMonth: 0,
    averageProcessingTime: 0
  };

  masterDataSections: MasterDataSection[] = [
    {
      id: 'suppliers',
      title: 'Lieferanten',
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
    {
      id: 'rules',
      title: 'Genehmigungs-Regeln',
      description: 'Automatisierungen verwalten',
      icon: 'schedule',
      route: '/admin/rules'
    },
    {
      id: 'workflows',
      title: 'Genehmigungs-Workflows',
      description: 'Abläufe konfigurieren',
      icon: 'schedule',
      route: '/admin/workflows'
    }
  ];

  constructor(
    private router: Router,
    private dashboardService: DashboardService,
    private systemConfigService: SystemConfigService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.configForm = this.fb.group({
      configKey: [{ value: '', disabled: true }, Validators.required],
      dataType: ['String', Validators.required],
      configValue: [''],
      description: [''],
      isEditable: [true]
    });
  }

  ngOnInit(): void {
    this.loadSystemStatus();
    this.loadConfigs();
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
        this.systemStatus = this.defaultSystemStatus;
        this.loading = false;
      }
    });
  }

  loadConfigs(): void {
    this.systemConfigService.getAll().subscribe({
      next: (configs) => {
        this.configs = configs;
        if (configs.length && !this.selectedConfig) {
          this.onSelectConfig(configs[0]);
        }
      },
      error: (error) => {
        console.error('Fehler beim Laden der Systemeinstellungen:', error);
        this.snackBar.open('Systemeinstellungen konnten nicht geladen werden.', 'OK', {
          duration: 4000
        });
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

  onSelectConfig(config: SystemConfig): void {
    this.selectedConfig = config;
    const parsedValue = this.parseConfigValue(config.configValue, config.dataType);

    this.configForm.reset({
      configKey: config.configKey,
      dataType: config.dataType,
      configValue: parsedValue,
      description: config.description ?? '',
      isEditable: config.isEditable
    });

    const valueControl = this.configForm.get('configValue');
    const typeControl = this.configForm.get('dataType');

    if (!config.isEditable) {
      valueControl?.disable({ emitEvent: false });
      typeControl?.disable({ emitEvent: false });
    } else {
      valueControl?.enable({ emitEvent: false });
      typeControl?.enable({ emitEvent: false });
    }
  }

  onTypeChanged(type: ConfigDataType): void {
    const current = this.configForm.getRawValue().configValue;

    switch (type) {
      case 'Number':
        this.configForm.patchValue({ configValue: typeof current === 'number' ? current : Number(current) || 0 });
        break;
      case 'Boolean':
        this.configForm.patchValue({ configValue: current === true || current === 'true' });
        break;
      case 'Json':
        this.configForm.patchValue({ configValue: this.stringifyJson(current) });
        break;
      default:
        this.configForm.patchValue({ configValue: current ?? '' });
    }
  }

  onSave(): void {
    if (!this.selectedConfig || !this.selectedConfig.isEditable) {
      this.snackBar.open('Diese Einstellung ist schreibgeschützt.', 'OK', { duration: 3000 });
      return;
    }

    if (this.configForm.invalid) {
      this.configForm.markAllAsTouched();
      return;
    }

    const raw = this.configForm.getRawValue();
    const preparedValue = this.formatValue(raw.configValue, raw.dataType);

    this.saving = true;

    this.systemConfigService
      .upsert(raw.configKey, {
        configKey: raw.configKey,
        configValue: preparedValue,
        dataType: raw.dataType,
        description: raw.description ?? null,
        isEditable: raw.isEditable
      })
      .subscribe({
        next: (updated) => {
          this.saving = false;
          this.snackBar.open('Systemeinstellung gespeichert.', 'OK', { duration: 3000 });
          this.replaceConfig(updated);
          this.onSelectConfig(updated);
        },
        error: (error) => {
          console.error('Fehler beim Speichern der Systemeinstellung:', error);
          this.saving = false;
          this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 4000 });
        }
      });
  }

  toggleAutoRule(rule: AutoApprovalRule): void {
    rule.isActive = !rule.isActive;
    console.log(`Auto-Regel ${rule.name} ${rule.isActive ? 'aktiviert' : 'deaktiviert'}`);
  }

  toggleAssignmentRule(rule: AssignmentRule): void {
    rule.isActive = !rule.isActive;
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

  private replaceConfig(updated: SystemConfig): void {
    const idx = this.configs.findIndex((c) => c.configKey === updated.configKey);
    if (idx >= 0) {
      this.configs[idx] = updated;
      this.configs = [...this.configs];
    } else {
      this.configs = [...this.configs, updated];
    }
  }

  private parseConfigValue(value: string | null, type: ConfigDataType): string | number | boolean | null {
    if (value == null) return null;
    switch (type) {
      case 'Number':
        return Number(value);
      case 'Boolean':
        return value === 'true' || value === '1';
      case 'Json':
        try {
          return JSON.stringify(JSON.parse(value), null, 2);
        } catch {
          return value;
        }
      default:
        return value;
    }
  }

  private formatValue(value: any, type: ConfigDataType): string | null {
    if (value === null || value === undefined || value === '') return null;
    switch (type) {
      case 'Number':
        return String(value);
      case 'Boolean':
        return value === true || value === 'true' ? 'true' : 'false';
      case 'Json':
        return this.stringifyJson(value);
      default:
        return String(value);
    }
  }

  private stringifyJson(value: any): string {
    if (typeof value === 'string') {
      try {
        return JSON.stringify(JSON.parse(value), null, 2);
      } catch {
        return value;
      }
    }
    try {
      return JSON.stringify(value, null, 2);
    } catch {
      return String(value ?? '');
    }
  }
}
