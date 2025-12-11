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
import { AuthService } from '../../core/services/auth.service';
import { AuthState } from '../../core/models/auth.models';
import { InvoiceService, Invoice, PagedResult } from '../../core/services/invoice.service';
import { catchError, finalize, of, filter, Subscription } from 'rxjs';

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

  tasks: UserTask[] = [];
  filteredTasks: UserTask[] = [];

  displayedColumns: string[] = ['status', 'supplier', 'amount', 'dueDate', 'action'];

  constructor(
    private authService: AuthService,
    private invoiceService: InvoiceService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.authState$.subscribe(authState => {
      this.currentUser = authState?.user;
      this.userPermissions = authState?.permissions || [];
    });

    this.loadUserTasks();

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

    // Simuliere API-Aufruf für User-spezifische Aufgaben
    // In Realität würde hier ein Service-Call zu einem UserTaskService gemacht werden
    this.invoiceService.getInvoices({ pageNumber: 0, pageSize: 50 }).pipe(
      catchError(error => {
        console.error('Fehler beim Laden der Aufgaben:', error);
        return of({ items: [], totalCount: 0, pageNumber: 0, pageSize: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
      }),
      finalize(() => this.loading = false)
    ).subscribe((response: PagedResult<Invoice>) => {
      this.processTasksData(response.items);
    });
  }

  private processTasksData(invoices: Invoice[]): void {
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
      statusText = 'EILT';
      statusClass = 'status-urgent';
      actionText = this.getActionButtonText();
      actionClass = 'action-urgent';
      reason = isEscalated ? 'Eskaliert' : 'Überfällig';
    } else if (isIncomplete) {
      status = 'incomplete';
      statusIcon = 'help_outline';
      statusText = 'Daten fehlen';
      statusClass = 'status-incomplete';
      actionText = 'Ergänzen';
      actionClass = 'action-incomplete';
      reason = 'Projekt fehlt';
    } else {
      status = 'normal';
      statusIcon = 'radio_button_unchecked';
      statusText = 'Offen';
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

  // Manual refresh method that can be called from UI
  refreshTasks(): void {
    console.log('Manual refresh triggered');
    this.loadUserTasks();
  }
  }

  private formatDueDate(dueDate: string): string {
    const date = new Date(dueDate);
    const today = new Date();
    const diffTime = date.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return 'Gestern';
    } else if (diffDays === 0) {
      return 'Heute';
    } else {
      return date.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' });
    }
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
    this.router.navigate(['/invoice', task.id]);
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    const firstName = this.currentUser?.firstName || 'User';

    if (hour < 12) {
      return `Guten Morgen, ${firstName}!`;
    } else if (hour < 18) {
      return `Guten Tag, ${firstName}!`;
    } else {
      return `Guten Abend, ${firstName}!`;
    }
  }

  getActionVerb(): string {
    // Admin und Freigeber sehen "freizugeben"
    if (this.userPermissions.includes('all') ||
        this.userPermissions.includes('approve_invoices')) {
      return 'freizugeben';
    }
    // Manager sehen "zu genehmigen"
    if (this.userPermissions.includes('approve_cost_center_invoices')) {
      return 'zu genehmigen';
    }
    // Buchhaltung und andere sehen "zu bearbeiten"
    if (this.userPermissions.includes('edit_invoices')) {
      return 'zu bearbeiten';
    }
    // Fallback für normale Benutzer
    return 'zu prüfen';
  }

  getActionButtonText(): string {
    // Admin und Freigeber sehen "Freigeben"
    if (this.userPermissions.includes('all') ||
        this.userPermissions.includes('approve_invoices')) {
      return 'Freigeben';
    }
    // Manager sehen "Genehmigen"
    if (this.userPermissions.includes('approve_cost_center_invoices')) {
      return 'Genehmigen';
    }
    // Buchhaltung und andere sehen "Bearbeiten"
    return 'Bearbeiten';
  }

  ngOnDestroy(): void {
    if (this.navigationSubscription) {
      this.navigationSubscription.unsubscribe();
    }
  }
}
