import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { environment } from '../../../environments/environment';
import { RoleDto, User } from '../../core/models/user.models';
import { TabConfig } from '../../core/interfaces/common.interfaces';
import { CreateSupplierDialogComponent } from './dialogs/create-supplier-dialog.component';
import { CreateCostCenterDialogComponent } from './dialogs/create-cost-center-dialog.component';
import { CreateProjectDialogComponent } from './dialogs/create-project-dialog.component';
import { CreatePurchaseOrderDialogComponent } from './dialogs/create-purchase-order-dialog.component';
import { CreateUserDialogComponent } from './dialogs/create-user-dialog.component';
import { EditSupplierDialogComponent } from './dialogs/edit-supplier-dialog.component';
import { EditCostCenterDialogComponent } from './dialogs/edit-cost-center-dialog.component';
import { EditProjectDialogComponent } from './dialogs/edit-project-dialog.component';
import { EditUserDialogComponent } from './dialogs/edit-user-dialog.component';
import { RoleDialogComponent } from './dialogs/role-dialog.component';
// Use the Admin Rules dialog for consistent rule UI
import { RuleDialogComponent } from '../smart-dashboard/dashboard/rule-dashboard/rule-dialog/rule-dialog.component';
import { ApprovalRule as AdminRule, RuleDialogData } from '../../core/models';
import { SupplierService } from '../../core/services/supplier.service';
import { CostCenterService } from '../../core/services/cost-center.service';
import { ProjectService } from '../../core/services/project.service';
import { UserService } from '../../core/services/user.service';
import { Supplier } from '../../core/models/supplier.model';
import { CostCenter } from '../../core/models/cost-center.model';
import { Project } from '../../core/models/project.model';
import { CreatePurchaseOrderRequest, PurchaseOrder } from '../../core/models/purchaseOrder.model';
import { PurchaseOrderService } from '../../core/services/purchase-order.service';
import { Invoice } from '../../core/models/invoice.models';
import { InvoiceService } from '../../core/services/invoice.service';
import { ApprovalService } from '../../core/services/approval.service';
import { ApprovalRule, ApprovalWorkflow, CreateApprovalRuleDto, CreateApprovalWorkflowDto, UpdateApprovalWorkflowDto } from '../../core/models/approval.model';
import { ApprovalWorkflowDialogComponent, ApprovalWorkflowDialogData } from './dialogs/approval-workflow-dialog.component';
import { EscalationRule, CreateEscalationRuleDto } from '../../core/models/escalation-rule.model';
import { EscalationRuleService } from '../../core/services/escalation-rule.service';
import { EscalationRuleDialogComponent, EscalationRuleDialogData } from './dialogs/escalation-rule-dialog.component';
import { RolesApiService } from '../../core/services/roles-api.service';

@Component({
  selector: 'app-master-data',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './master-data.component.html',
  styleUrls: ['./master-data.component.scss'],
})
export class MasterDataComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  // Default colors for common roles when backend color is missing
  private roleColorMap: Record<string, string> = {
    administrator: '#F44336',
    admin: '#F44336',
    benutzer: '#2196F3',
    user: '#2196F3',
    buchhaltung: '#4CAF50',
    accounting: '#4CAF50',
    manager: '#FF9800',
    freigeber: '#FF9800',
    mitarbeiter: '#795548',
  };

  // Data arrays
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];
  purchaseOrders: PurchaseOrder[] = [];
  invoices: Invoice[] = [];
  users: User[] = [];
  roles: RoleDto[] = [];
  approvalRules: ApprovalRule[] = [];
  approvalWorkflows: ApprovalWorkflow[] = [];
  escalationRules: EscalationRule[] = [];

  // Loading states
  loadingSuppliers = false;
  loadingCostCenters = false;
  loadingProjects = false;
  loadingPurchaseOrders = false;
  loadingInvoices = false;
  loadingUsers = false;
  loadingRoles = false;
  loadingApprovalRules = false;
  loadingApprovalWorkflows = false;
  loadingEscalationRules = false;

  // Tab management
  activeTab = 0;
  tabData: TabConfig[] = [
    { id: 'suppliers', label: 'Lieferanten', index: 0 },
    { id: 'costcenters', label: 'Kostenstellen', index: 1 },
    { id: 'projects', label: 'Projekte', index: 2 },
    { id: 'purchaseorders', label: 'Bestellungen', index: 3 },
    { id: 'invoices', label: 'Rechnungen', index: 4 },
    { id: 'users', label: 'Benutzer', index: 5 },
    { id: 'roles', label: 'Rollen', index: 6 },
    { id: 'escalation', label: 'Eskalations-Einstellungen', index: 7 },
    { id: 'rules', label: 'Genehmigungsregeln', index: 8 },
    { id: 'workflows', label: 'Genehmigungsworkflows', index: 9 },
  ];

  invoiceStatuses = [
    'Eingegangen',
    'In_Pruefung',
    'Freigabe_Erforderlich',
    'Freigegeben',
    'Abgelehnt',
    'Bezahlt',
    'Ueberfaellig',
    'Storniert'
  ];

  // Table columns
  supplierColumns = ['id', 'name', 'email', 'phone', 'isActive', 'actions'];
  costCenterColumns = [
    'id',
    'name',
    'description',
    'budget',
    'isActive',
    'actions',
  ];
  projectColumns = ['id', 'name', 'costCenter', 'budget', 'status', 'actions'];
  purchaseOrderColumns = [
    'id',
    'title',
    'costCenter',
    'project',
    'totalAmount',
    'status',
    'createdAt',
    'actions',
  ];
  invoiceColumns = [
    'id',
    'invoiceNumber',
    'supplier',
    'totalAmount',
    'status',
    'invoiceDate',
    'actions',
  ];
  userColumns = [
    'username',
    'fullName',
    'email',
    'role',
    'isActive',
    'lastLogin',
    'actions',
  ];
  roleColumns = ['color', 'name', 'description', 'permissions', 'isSystemRole', 'actions'];
  approvalRuleColumns = [
    'id',
    'name',
    'description',
    'ruleType',
    'priority',
    'isActive',
    'actions',
  ];
  escalationColumns = [
    'id',
    'name',
    'triggerStatus',
    'triggerAfterHours',
    'repeatIntervalHours',
    'maxEscalations',
    'notify',
    'isActive',
    'actions',
  ];
  approvalWorkflowColumns = [
    'id',
    'invoiceId',
    'approverId',
    'approvalLevel',
    'status',
    'createdAt',
    'actions',
  ];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private route: ActivatedRoute,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService,
    private purchaseOrderService: PurchaseOrderService,
    private invoiceService: InvoiceService,
    private userService: UserService,
    private approvalService: ApprovalService,
    private escalationRuleService: EscalationRuleService,
    private rolesApi: RolesApiService
  ) {}

  ngOnInit(): void {
    // Set active tab based on route parameter
    const activeTabParam = this.route.snapshot.data['activeTab'];
    if (activeTabParam) {
      const tabIndex = this.tabData.find(
        (tab) => tab.id === activeTabParam
      )?.index;
      if (tabIndex !== undefined) {
        this.activeTab = tabIndex;
      }
    }

    this.loadAllData();
  }

  loadAllData(): void {
    this.loadSuppliers();
    this.loadCostCenters();
    this.loadProjects();
    this.loadPurchaseOrders();
    this.loadInvoices();
    this.loadUsers();
    this.loadRoles();
    this.loadApprovalRules();
    this.loadApprovalWorkflows();
    this.loadEscalationRules();
  }

  // Suppliers
  loadSuppliers(): void {
    this.loadingSuppliers = true;
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
        this.loadingSuppliers = false;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
        this.loadingSuppliers = false;
        this.snackBar.open('Fehler beim Laden der Lieferanten', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteSupplier(id: number): void {
    if (confirm('Möchten Sie diesen Lieferanten wirklich löschen?')) {
      this.supplierService.deleteSupplier(id).subscribe({
        next: () => {
          this.snackBar.open('Lieferant gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadSuppliers();
        },
        error: (error) => {
          console.error('Error deleting supplier:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Cost Centers
  loadCostCenters(): void {
    this.loadingCostCenters = true;
    this.costCenterService.getCostCenters().subscribe({
      next: (data) => {
        this.costCenters = data;
        this.loadingCostCenters = false;
      },
      error: (error) => {
        console.error('Error loading cost centers:', error);
        this.loadingCostCenters = false;
        this.snackBar.open('Fehler beim Laden der Kostenstellen', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteCostCenter(id: string): void {
    if (confirm('Möchten Sie diese Kostenstelle wirklich löschen?')) {
      this.costCenterService.deleteCostCenter(id).subscribe({
        next: () => {
          this.snackBar.open('Kostenstelle gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadCostCenters();
        },
        error: (error) => {
          console.error('Error deleting cost center:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Projects
  loadProjects(): void {
    this.loadingProjects = true;
    this.projectService.getProjects().subscribe({
      next: (data) => {
        this.projects = data;
        this.loadingProjects = false;
      },
      error: (error) => {
        console.error('Error loading projects:', error);
        this.loadingProjects = false;
        this.snackBar.open('Fehler beim Laden der Projekte', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteProject(id: string): void {
    if (confirm('Möchten Sie dieses Projekt wirklich löschen?')) {
      this.projectService.deleteProject(id).subscribe({
        next: () => {
          this.snackBar.open('Projekt gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadProjects();
        },
        error: (error) => {
          console.error('Error deleting project:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Purchase Orders
  loadPurchaseOrders(): void {
    this.loadingPurchaseOrders = true;
    this.purchaseOrderService.getPurchaseOrders().subscribe({
      next: (data) => {
        this.purchaseOrders = data;
        this.loadingPurchaseOrders = false;
      },
      error: (error) => {
        console.error('Error loading purchase orders:', error);
        this.loadingPurchaseOrders = false;
        this.snackBar.open('Fehler beim Laden der Bestellungen', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deletePurchaseOrder(id: string): void {
    if (confirm('Möchten Sie diese Bestellung wirklich löschen?')) {
      this.purchaseOrderService.deletePurchaseOrder(id).subscribe({
        next: () => {
          this.snackBar.open('Bestellung gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadPurchaseOrders();
        },
        error: (error) => {
          console.error('Error deleting purchase order:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Invoices
  loadInvoices(): void {
    this.loadingInvoices = true;
    this.invoiceService.getInvoices().subscribe({
      next: (data) => {
        console.log('Loaded invoices:', data);
        this.invoices = data.items; // Use the array of invoices from the paged result
        this.loadingInvoices = false;
      },
      error: (error) => {
        console.error('Error loading invoices:', error);
        this.loadingInvoices = false;
        this.snackBar.open('Fehler beim Laden der Rechnungen', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteInvoice(id: number): void {
    if (confirm('Möchten Sie diese Rechnung wirklich löschen?')) {
      this.invoiceService.deleteInvoice(id).subscribe({
        next: () => {
          this.snackBar.open('Rechnung gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadInvoices();
        },
        error: (error) => {
          console.error('Error deleting invoice:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Users
  loadUsers(): void {
    this.loadingUsers = true;
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data.map((u) => ({
          ...u,
          roles: (u.roles || []).map((r) => ({
            ...r,
            color: r.color || this.getDefaultRoleColor(r.name),
          })),
        }));
        this.loadingUsers = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.loadingUsers = false;
        this.snackBar.open('Fehler beim Laden der Benutzer', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteUser(id: string): void {
    if (confirm('Möchten Sie diesen Benutzer wirklich löschen?')) {
      this.userService.deleteUser(id).subscribe({
        next: () => {
          this.snackBar.open('Benutzer gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadUsers();
        },
        error: (error) => {
          console.error('Error deleting user:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Roles
  loadRoles(): void {
    this.loadingRoles = true;
    this.rolesApi.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.loadingRoles = false;
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.loadingRoles = false;
        this.snackBar.open('Fehler beim Laden der Rollen', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  createRole(): void {
    const dialogRef = this.dialog.open(RoleDialogComponent, {
      width: '520px',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed().subscribe((payload) => {
      if (payload) {
        this.rolesApi.createRole(payload).subscribe({
          next: () => {
            this.snackBar.open('Rolle erstellt', 'Schließen', { duration: 3000 });
            this.loadRoles();
          },
          error: (error) => {
            console.error('Error creating role:', error);
            this.snackBar.open('Fehler beim Erstellen der Rolle', 'Schließen', {
              duration: 3000,
            });
          },
        });
      }
    });
  }

  editRole(role: RoleDto): void {
    if (!role) {
      return;
    }
    const dialogRef = this.dialog.open(RoleDialogComponent, {
      width: '520px',
      data: { mode: 'edit', role },
    });

    dialogRef.afterClosed().subscribe((payload) => {
      if (payload) {
        this.rolesApi.updateRole(role.id, payload).subscribe({
          next: () => {
            this.snackBar.open('Rolle aktualisiert', 'Schließen', { duration: 3000 });
            this.loadRoles();
          },
          error: (error) => {
            console.error('Error updating role:', error);
            this.snackBar.open('Fehler beim Aktualisieren der Rolle', 'Schließen', {
              duration: 3000,
            });
          },
        });
      }
    });
  }

  deleteRole(role: RoleDto): void {
    if (role.isSystemRole) {
      this.snackBar.open('Systemrollen können nicht gelöscht werden', 'Schließen', {
        duration: 3000,
      });
      return;
    }

    if (!confirm(`Rolle "${role.name}" wirklich löschen?`)) {
      return;
    }

    this.rolesApi.deleteRole(role.id).subscribe({
      next: () => {
        this.snackBar.open('Rolle gelöscht', 'Schließen', { duration: 3000 });
        this.loadRoles();
      },
      error: (error) => {
        console.error('Error deleting role:', error);
        this.snackBar.open('Fehler beim Löschen der Rolle', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  // Create Methods
  createSupplier(): void {
    const dialogData: Supplier = {
      id: 0,
      name: '',
    };

    const dialogRef = this.dialog.open(CreateSupplierDialogComponent, {
      width: '600px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.supplierService.createSupplier(result).subscribe({
          next: () => {
            this.snackBar.open('Lieferant erfolgreich erstellt', 'Schließen', {
              duration: 3000,
            });
            this.loadSuppliers();
          },
          error: (error) => {
            console.error('Error creating supplier:', error);
            this.snackBar.open(
              'Fehler beim Erstellen des Lieferanten',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  createCostCenter(): void {
    const dialogData: CostCenter = {
      id: '',
      name: '',
      budget: 0,
    };

    const dialogRef = this.dialog.open(CreateCostCenterDialogComponent, {
      width: '500px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.costCenterService.addCostCenter(result).subscribe({
          next: () => {
            this.snackBar.open(
              'Kostenstelle erfolgreich erstellt',
              'Schließen',
              { duration: 3000 }
            );
            this.loadCostCenters();
          },
          error: (error) => {
            console.error('Error creating cost center:', error);
            this.snackBar.open(
              'Fehler beim Erstellen der Kostenstelle',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  createProject(): void {
    const dialogData: Project = {
      id: '',
      name: '',
      costCenterId: this.costCenters.length > 0 ? this.costCenters[0].id : '',
      budget: 0,
      status: 'Geplant',
    };

    const dialogRef = this.dialog.open(CreateProjectDialogComponent, {
      width: '600px',
      data: {
        project: dialogData,
        costCenters: this.costCenters,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.projectService.addProject(result).subscribe({
          next: () => {
            this.snackBar.open('Projekt erfolgreich erstellt', 'Schließen', {
              duration: 3000,
            });
            this.loadProjects();
          },
          error: (error) => {
            console.error('Error creating project:', error);
            this.snackBar.open(
              'Fehler beim Erstellen des Projekts',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editPurchaseOrder(order: PurchaseOrder): void {
    const dialogRef = this.dialog.open(CreatePurchaseOrderDialogComponent, {
      width: '650px',
      data: {
        costCenters: this.costCenters,
        projects: this.projects,
      },
    });

    // Seed the form by setting initial value after component init
    dialogRef.afterOpened().subscribe(() => {
      const instance = dialogRef.componentInstance;
      if (instance) {
        instance.form.patchValue({
          id: order.id,
          title: order.title,
          description: order.description || '',
          costCenterId: order.costCenterId || '',
          projectId: order.projectId || '',
          totalAmount: order.totalAmount,
          currency: order.currency || 'EUR',
        });
      }
    });

    dialogRef.afterClosed().subscribe((payload: CreatePurchaseOrderRequest | undefined) => {
      if (payload) {
        this.purchaseOrderService.updatePurchaseOrder(order.id, payload).subscribe({
          next: () => {
            this.snackBar.open('Bestellung aktualisiert', 'Schließen', { duration: 3000 });
            this.loadPurchaseOrders();
          },
          error: (error) => {
            console.error('Error updating purchase order:', error);
            this.snackBar.open('Fehler beim Aktualisieren der Bestellung', 'Schließen', {
              duration: 3000,
            });
          },
        });
      }
    });
  }
  createUser(): void {
    const dialogData: User = {
      id: '',
      username: '',
      email: '',
      firstName: '',
      lastName: '',
      role: '',
      isActive: false,
      createdAt: '',
    };

    const dialogRef = this.dialog.open(CreateUserDialogComponent, {
      width: '500px',
      data: { user: dialogData, availableRoles: this.roles },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.userService.addUser(result).subscribe({
          next: () => {
            this.snackBar.open('Benutzer erfolgreich erstellt', 'Schließen', {
              duration: 3000,
            });
            this.loadUsers();
          },
          error: (error) => {
            console.error('Error creating user:', error);
            this.snackBar.open(
              'Fehler beim Erstellen des Benutzers',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  createEscalationRule(): void {
    const dialogRef = this.dialog.open(EscalationRuleDialogComponent, {
      width: '700px',
      data: {
        mode: 'create',
        statuses: this.invoiceStatuses,
      } as EscalationRuleDialogData,
    });

    dialogRef.afterClosed().subscribe((result: CreateEscalationRuleDto | undefined) => {
      if (result) {
        const payload: CreateEscalationRuleDto = {
          ...result,
          triggerAfterHours: Number(result.triggerAfterHours),
          repeatIntervalHours: result.repeatIntervalHours ? Number(result.repeatIntervalHours) : null,
          maxEscalations: result.maxEscalations !== undefined && result.maxEscalations !== null
            ? Number(result.maxEscalations)
            : null,
          notifyUserId: result.notifyUserId ? Number(result.notifyUserId) : null,
          isActive: result.isActive ?? true,
        };

        this.escalationRuleService.createRule(payload).subscribe({
          next: () => {
            this.snackBar.open('Eskalationsregel erstellt', 'Schließen', { duration: 3000 });
            this.loadEscalationRules();
          },
          error: (error) => {
            console.error('Error creating escalation rule:', error);
            this.snackBar.open('Fehler beim Erstellen der Eskalationsregel', 'Schließen', { duration: 3000 });
          },
        });
      }
    });
  }

  editEscalationRule(rule: EscalationRule): void {
    const dialogRef = this.dialog.open(EscalationRuleDialogComponent, {
      width: '700px',
      data: {
        mode: 'edit',
        statuses: this.invoiceStatuses,
        rule,
      } as EscalationRuleDialogData,
    });

    dialogRef.afterClosed().subscribe((result: CreateEscalationRuleDto | undefined) => {
      if (result) {
        const payload: CreateEscalationRuleDto = {
          ...result,
          triggerAfterHours: Number(result.triggerAfterHours),
          repeatIntervalHours: result.repeatIntervalHours ? Number(result.repeatIntervalHours) : null,
          maxEscalations: result.maxEscalations !== undefined && result.maxEscalations !== null
            ? Number(result.maxEscalations)
            : null,
          notifyUserId: result.notifyUserId ? Number(result.notifyUserId) : null,
          isActive: result.isActive ?? rule.isActive,
        };

        this.escalationRuleService.updateRule(rule.id, payload).subscribe({
          next: () => {
            this.snackBar.open('Eskalationsregel aktualisiert', 'Schließen', { duration: 3000 });
            this.loadEscalationRules();
          },
          error: (error) => {
            console.error('Error updating escalation rule:', error);
            this.snackBar.open('Fehler beim Aktualisieren der Eskalationsregel', 'Schließen', { duration: 3000 });
          },
        });
      }
    });
  }

  deleteEscalationRule(rule: EscalationRule): void {
    if (!confirm(`Eskalationsregel "${rule.name}" deaktivieren?`)) {
      return;
    }

    this.escalationRuleService.deleteRule(rule.id).subscribe({
      next: () => {
        this.snackBar.open('Eskalationsregel deaktiviert', 'Schließen', { duration: 3000 });
        this.loadEscalationRules();
      },
      error: (error) => {
        console.error('Error deleting escalation rule:', error);
        this.snackBar.open('Fehler beim Deaktivieren der Eskalationsregel', 'Schließen', { duration: 3000 });
      },
    });
  }

  // Edit Methods
  editSupplier(supplier: Supplier): void {
    const dialogRef = this.dialog.open(EditSupplierDialogComponent, {
      width: '600px',
      data: supplier,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.supplierService.updateSupplier(supplier.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              'Lieferant erfolgreich aktualisiert',
              'Schließen',
              { duration: 3000 }
            );
            this.loadSuppliers();
          },
          error: (error) => {
            console.error('Error updating supplier:', error);
            this.snackBar.open(
              'Fehler beim Aktualisieren des Lieferanten',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editCostCenter(costCenter: CostCenter): void {
    const dialogRef = this.dialog.open(EditCostCenterDialogComponent, {
      width: '500px',
      data: costCenter,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.costCenterService
          .updateCostCenter(costCenter.id, result)
          .subscribe({
            next: () => {
              this.snackBar.open(
                'Kostenstelle erfolgreich aktualisiert',
                'Schließen',
                { duration: 3000 }
              );
              this.loadCostCenters();
            },
            error: (error) => {
              console.error('Error updating cost center:', error);
              this.snackBar.open(
                'Fehler beim Aktualisieren der Kostenstelle',
                'Schließen',
                { duration: 3000 }
              );
            },
          });
      }
    });
  }

  editProject(project: Project): void {
    const dialogRef = this.dialog.open(EditProjectDialogComponent, {
      width: '600px',
      data: {
        project: project,
        costCenters: this.costCenters,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.projectService.updateProject(project.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              'Projekt erfolgreich aktualisiert',
              'Schließen',
              { duration: 3000 }
            );
            this.loadProjects();
          },
          error: (error) => {
            console.error('Error updating project:', error);
            this.snackBar.open(
              'Fehler beim Aktualisieren des Projekts',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editUser(user: User): void {
    const dialogRef = this.dialog.open(EditUserDialogComponent, {
      width: '500px',
      data: { user, availableRoles: this.roles },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.userService.updateUser(user.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              'Benutzer erfolgreich aktualisiert',
              'Schließen',
              { duration: 3000 }
            );
            this.loadUsers();
          },
          error: (error) => {
            console.error('Error updating user:', error);
            this.snackBar.open(
              'Fehler beim Aktualisieren des Benutzers',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  // Format currency
  formatCurrency(amount: number, currency = 'EUR'): string {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: currency || 'EUR',
    }).format(amount);
  }

  // Format date
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('de-DE');
  }

  private getDefaultRoleColor(roleName?: string): string {
    if (!roleName) {
      return '#ff9800';
    }
    const key = roleName.trim().toLowerCase();
    return this.roleColorMap[key] || '#ff9800';
  }

  // Approval Rules
  loadApprovalRules(): void {
    this.loadingApprovalRules = true;
    this.approvalService.getApprovalRules().subscribe({
      next: (rules) => {
        this.approvalRules = rules;
        this.loadingApprovalRules = false;
      },
      error: (error) => {
        console.error('Error loading approval rules:', error);
        this.loadingApprovalRules = false;
        this.snackBar.open('Fehler beim Laden der Genehmigungsregeln', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  loadEscalationRules(): void {
    this.loadingEscalationRules = true;
    this.escalationRuleService.getRules().subscribe({
      next: (rules) => {
        this.escalationRules = rules;
        this.loadingEscalationRules = false;
      },
      error: (error) => {
        console.error('Error loading escalation rules:', error);
        this.loadingEscalationRules = false;
        this.snackBar.open('Fehler beim Laden der Eskalationsregeln', 'Schließen', { duration: 3000 });
      },
    });
  }

  createApprovalRule(): void {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { mode: 'create' } as RuleDialogData,
    });

    dialogRef.afterClosed().subscribe((result: AdminRule | undefined) => {
      if (result) {
        const payload: CreateApprovalRuleDto = this.mapFromDialogRule(result);
        this.approvalService.createApprovalRule(payload).subscribe({
          next: () => {
            this.snackBar.open('Genehmigungsregel erfolgreich erstellt', 'Schließen', {
              duration: 3000,
            });
            this.loadApprovalRules();
          },
          error: (error) => {
            console.error('Error creating approval rule:', error);
            this.snackBar.open('Fehler beim Erstellen der Genehmigungsregel', 'Schließen', {
              duration: 3000,
            });
          },
        });
      }
    });
  }

  editApprovalRule(rule: ApprovalRule): void {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      width: '800px',
      data: { mode: 'edit', rule: this.mapToDialogRule(rule) } as RuleDialogData,
    });

    dialogRef.afterClosed().subscribe((result: AdminRule | undefined) => {
      if (result) {
        const payload: CreateApprovalRuleDto = this.mapFromDialogRule(result);
        this.approvalService.updateApprovalRule(rule.id, payload).subscribe({
          next: () => {
            this.snackBar.open('Genehmigungsregel erfolgreich aktualisiert', 'Schließen', {
              duration: 3000,
            });
            this.loadApprovalRules();
          },
          error: (error) => {
            console.error('Error updating approval rule:', error);
            this.snackBar.open('Fehler beim Aktualisieren der Genehmigungsregel', 'Schließen', {
              duration: 3000,
            });
          },
        });
      }
    });
  }

  deleteApprovalRule(id: number): void {
    if (confirm('Möchten Sie diese Genehmigungsregel wirklich löschen?')) {
      this.approvalService.deleteApprovalRule(id).subscribe({
        next: () => {
          this.snackBar.open('Genehmigungsregel gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadApprovalRules();
        },
        error: (error) => {
          console.error('Error deleting approval rule:', error);
          this.snackBar.open('Fehler beim Löschen der Genehmigungsregel', 'Schließen', {
            duration: 3000,
          });
        },
      });
    }
  }

  // Mapping helpers for Admin Rule dialog <-> API DTOs
  private mapToDialogRule(rule: ApprovalRule): AdminRule {
    let conditions: any[] = [];
    let actions: any[] = [];
    try {
      conditions = rule.conditions ? JSON.parse(rule.conditions as unknown as string) : [];
    } catch {}
    try {
      actions = rule.actions ? JSON.parse(rule.actions as unknown as string) : [];
    } catch {}
    const apiType = typeof rule.ruleType === 'string' ? rule.ruleType.toLowerCase() : '';
    const dialogType: 'automatic' | 'manual' = apiType === 'automatic' ? 'automatic' : 'manual';
    return {
      id: rule.id,
      name: rule.name,
      description: rule.description || '',
      isActive: rule.isActive,
      ruleType: dialogType,
      conditions: conditions,
      actions: actions,
      priority: rule.priority ?? 10,
    } as AdminRule;
  }

  private mapFromDialogRule(rule: AdminRule): CreateApprovalRuleDto {
    const apiType = rule.ruleType === 'automatic' ? 'Automatic' : 'Manual';
    return {
      name: rule.name,
      description: rule.description,
      ruleType: apiType,
      priority: rule.priority,
      conditions: JSON.stringify(rule.conditions || []),
      actions: JSON.stringify(rule.actions || []),
    } as CreateApprovalRuleDto;
  }

  // Approval Workflows
  loadApprovalWorkflows(): void {
    this.loadingApprovalWorkflows = true;
    this.approvalService.getApprovalWorkflows().subscribe({
      next: (workflows) => {
        this.approvalWorkflows = workflows;
        this.loadingApprovalWorkflows = false;
      },
      error: (error) => {
        console.error('Error loading approval workflows:', error);
        this.loadingApprovalWorkflows = false;
        this.snackBar.open('Fehler beim Laden der Genehmigungsworkflows', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  deleteApprovalWorkflow(id: number): void {
    if (confirm('Möchten Sie diesen Genehmigungsworkflow wirklich löschen?')) {
      this.approvalService.deleteApprovalWorkflow(id).subscribe({
        next: () => {
          this.snackBar.open('Workflow gelöscht', 'Schließen', { duration: 3000 });
          this.loadApprovalWorkflows();
        },
        error: (error) => {
          console.error('Error deleting workflow:', error);
          this.snackBar.open('Fehler beim Löschen des Workflows', 'Schließen', { duration: 3000 });
        },
      });
    }
  }

  createApprovalWorkflow(): void {
    const dialogRef = this.dialog.open(ApprovalWorkflowDialogComponent, {
      width: '650px',
      data: { mode: 'create' } as ApprovalWorkflowDialogData,
    });

    dialogRef.afterClosed().subscribe((payload: CreateApprovalWorkflowDto | undefined) => {
      if (payload) {
        this.approvalService.createApprovalWorkflow(payload).subscribe({
          next: () => {
            this.snackBar.open('Workflow erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error) => {
            console.error('Error creating workflow:', error);
            this.snackBar.open('Fehler beim Erstellen des Workflows', 'Schließen', { duration: 3000 });
          },
        });
      }
    });
  }

  editApprovalWorkflow(workflow: ApprovalWorkflow): void {
    const dialogRef = this.dialog.open(ApprovalWorkflowDialogComponent, {
      width: '650px',
      data: { mode: 'edit', workflow } as ApprovalWorkflowDialogData,
    });

    dialogRef.afterClosed().subscribe((payload: UpdateApprovalWorkflowDto | undefined) => {
      if (payload) {
        this.approvalService.updateApprovalWorkflow(workflow.id, payload).subscribe({
          next: () => {
            this.snackBar.open('Workflow erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
            this.loadApprovalWorkflows();
          },
          error: (error) => {
            console.error('Error updating workflow:', error);
            this.snackBar.open('Fehler beim Aktualisieren des Workflows', 'Schließen', { duration: 3000 });
          },
        });
      }
    });
  }
}
