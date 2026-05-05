import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../../../core/services/auth.service';
import { AuthState } from '../../../../core/models/auth.models';
import { InvoiceService, PagedResult } from '../../../../core/services/invoice.service';
import { Invoice } from '../../../../core/models';
import { catchError, finalize, of, filter, Subscription, skip } from 'rxjs';
import { LanguageService } from '../../../../core/services/language.service';

interface UserTask {
  id: number;
  status: 'urgent' | 'incomplete' | 'normal';
  statusIcon: string;
  statusText: string;
  statusClass: string;
  supplierName: string;
  amount: string;
  dueDate: string;
  actionText: string;
  actionClass: string;
  reason?: string;
}

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.component.html',
  styleUrls: ['./user-dashboard.component.scss'],
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatMenuModule,
    MatChipsModule,
    MatBadgeModule
  ]
})
export class UserDashboardComponent implements OnInit, OnDestroy {
  loading = false;
  currentUser: any = null;
  userPermissions: string[] = [];
  private navigationSubscription?: Subscription;

  urgentCount = 0;
  incompleteCount = 0;
  totalTasks = 0;

  currentFilter = 'all';

  private sourceInvoices: Invoice[] = [];
  tasks: UserTask[] = [];
  filteredTasks: UserTask[] = [];

  displayedColumns: string[] = ['status', 'supplier', 'amount', 'dueDate', 'action'];

  constructor(
    private authService: AuthService,
    private invoiceService: InvoiceService,
    private router: Router,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.authService.authState$.subscribe(authState => {
      this.currentUser = authState?.user;
      this.userPermissions = authState?.permissions || [];
    });

    this.loadUserTasks();

    this.languageService.currentLanguage$.pipe(skip(1)).subscribe(() => {
      this.processTasksData(this.sourceInvoices);
    });

    // Listen for navigation events to refresh data when returning to dashboard
    this.navigationSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // Refresh data when navigating to dashboard
      if (event.url.includes('/dashboard')) {
        console.log('Dashboard navigation detected, refreshing tasks...');
        this.loadUserTasks();
      }
    });
  }

  private loadUserTasks(): void {
    this.loading = true;

    // Get invoices that require the current user's approval
    this.invoiceService.getPendingApprovals().pipe(
      catchError(error => {
        console.error('Fehler beim Laden der Aufgaben:', error);
        return of([]);
      }),
      finalize(() => this.loading = false)
    ).subscribe((invoices: Invoice[]) => {
      this.processTasksData(invoices);
    });
  }

  private processTasksData(invoices: Invoice[]): void {
    this.sourceInvoices = [...invoices];
    // All returned invoices require approval from the current user
    this.tasks = invoices.map(invoice => this.mapInvoiceToTask(invoice));
    this.calculateCounts();
    this.applyFilter(this.currentFilter);
  }

  private mapInvoiceToTask(invoice: Invoice): UserTask {
    const isOverdue = new Date(invoice.dueDate) < new Date();
    const isIncomplete = !invoice.costCenterId || !invoice.projectId;
    const isEscalated = invoice.isOverdue && invoice.daysOverdue > 3; // Simuliere Eskalation

    let status: 'urgent' | 'incomplete' | 'normal';
    let statusIcon: string;
    let statusText: string;
    let statusClass: string;
    let actionText: string;
    let actionClass: string;
    let reason: string | undefined;

    if (isOverdue || isEscalated) {
      status = 'urgent';
      statusIcon = 'warning';
      statusText = this.t('dashboard.user.status.urgent');
      statusClass = 'status-urgent';
      actionText = this.getActionButtonText();
      actionClass = 'action-urgent';
      reason = isEscalated
        ? this.t('dashboard.user.reason.escalated')
        : this.t('dashboard.user.reason.overdue');
    } else if (isIncomplete) {
      status = 'incomplete';
      statusIcon = 'help_outline';
      statusText = this.t('dashboard.user.status.incomplete');
      statusClass = 'status-incomplete';
      actionText = this.t('dashboard.user.action.complete');
      actionClass = 'action-incomplete';
      reason = this.t('dashboard.user.reason.projectMissing');
    } else {
      status = 'normal';
      statusIcon = 'radio_button_unchecked';
      statusText = this.t('dashboard.user.status.open');
      statusClass = 'status-normal';
      actionText = this.getActionButtonText();
      actionClass = 'action-normal';
    }

    return {
      id: invoice.id,
      status,
      statusIcon,
      statusText,
      statusClass,
      supplierName: invoice.supplier.name,
      amount: `€ ${invoice.totalAmount.toLocaleString('de-DE')},-`,
      dueDate: this.formatDueDate(invoice.dueDate),
      actionText,
      actionClass,
      reason
    };
  }

  // Manual refresh method that can be called from UI
  refreshTasks(): void {
    console.log('Manual refresh triggered');
    this.loadUserTasks();
  }

  private formatDueDate(dueDate: string): string {
    const date = new Date(dueDate);
    return date.toLocaleDateString('de-DE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private calculateCounts(): void {
    this.urgentCount = this.tasks.filter(task => task.status === 'urgent').length;
    this.incompleteCount = this.tasks.filter(task => task.status === 'incomplete').length;
    this.totalTasks = this.tasks.length;
  }

  applyFilter(filter: string): void {
    this.currentFilter = filter;

    switch (filter) {
      case 'urgent':
        this.filteredTasks = this.tasks.filter(task => task.status === 'urgent');
        break;
      case 'waiting':
        this.filteredTasks = this.tasks.filter(task => task.status === 'incomplete');
        break;
      case 'all':
      default:
        this.filteredTasks = [...this.tasks];
        break;
    }
  }

  onTaskAction(task: UserTask): void {
    // Weiterleitung zur Bearbeitung der Rechnung
    console.log('Navigating to invoice:', task.id);
    this.router.navigate(['/invoice', task.id]).then(
      success => console.log('Navigation successful:', success),
      error => console.error('Navigation failed:', error)
    );
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    const firstName = this.currentUser?.firstName || 'User';

    if (hour < 12) {
      return this.t('dashboard.user.greeting.morning').replace('{name}', firstName);
    } else if (hour < 18) {
      return this.t('dashboard.user.greeting.day').replace('{name}', firstName);
    } else {
      return this.t('dashboard.user.greeting.evening').replace('{name}', firstName);
    }
  }

  getActionVerb(): string {
    // Admin und Freigeber sehen "freizugeben"
    if (this.userPermissions.includes('invoices.approve')) {
      return this.t('dashboard.user.verb.approve');
    }
    // Manager sehen "zu genehmigen"
    if (this.userPermissions.includes('invoices.approve_cost_center')) {
      return this.t('dashboard.user.verb.authorize');
    }
    // Buchhaltung und andere sehen "zu bearbeiten"
    if (this.userPermissions.includes('invoices.edit')) {
      return this.t('dashboard.user.verb.edit');
    }
    // Fallback für normale Benutzer
    return this.t('dashboard.user.verb.review');
  }

  getActionButtonText(): string {
    // Admin und Freigeber sehen "Freigeben"
    if (this.userPermissions.includes('invoices.approve')) {
      return this.t('dashboard.user.button.approve');
    }
    // Manager sehen "Genehmigen"
    if (this.userPermissions.includes('invoices.approve_cost_center')) {
      return this.t('dashboard.user.button.authorize');
    }
    // Buchhaltung und andere sehen "Bearbeiten"
    return this.t('dashboard.user.button.edit');
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }

  getInvoiceLabel(): string {
    return this.totalTasks === 1
      ? this.t('dashboard.user.invoice.single')
      : this.t('dashboard.user.invoice.plural');
  }

  ngOnDestroy(): void {
    if (this.navigationSubscription) {
      this.navigationSubscription.unsubscribe();
    }
  }
}
