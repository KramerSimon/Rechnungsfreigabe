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
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../environments/environment';
// Zentrale Modelle
import {
  Supplier,
  CostCenter,
  Project,
  PurchaseOrder,
  CreateSupplierData,
  CreateCostCenterData,
  CreateProjectData
} from '../../core/models/master-data.models';
import {
  User,
  CreateUserData
} from '../../core/models/user.models';
import {
  TabConfig
} from '../../core/interfaces/common.interfaces';
import { CreateSupplierDialogComponent } from './dialogs/create-supplier-dialog.component';
import { CreateCostCenterDialogComponent } from './dialogs/create-cost-center-dialog.component';
import { CreateProjectDialogComponent } from './dialogs/create-project-dialog.component';
import { CreateUserDialogComponent } from './dialogs/create-user-dialog.component';
import { EditSupplierDialogComponent } from './dialogs/edit-supplier-dialog.component';
import { EditCostCenterDialogComponent } from './dialogs/edit-cost-center-dialog.component';
import { EditProjectDialogComponent } from './dialogs/edit-project-dialog.component';
import { EditUserDialogComponent } from './dialogs/edit-user-dialog.component';
import { RoleManagementDialogComponent } from './dialogs/role-management-dialog.component';

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
    ReactiveFormsModule
  ],
  templateUrl: './master-data.component.html',
  styleUrls: ['./master-data.component.scss']
})
export class MasterDataComponent implements OnInit {
  private apiUrl = environment.apiUrl;

  // Data arrays
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];
  purchaseOrders: PurchaseOrder[] = [];
  users: User[] = [];

  // Loading states
  loadingSuppliers = false;
  loadingCostCenters = false;
  loadingProjects = false;
  loadingPurchaseOrders = false;
  loadingUsers = false;

  // Tab management
  activeTab = 0;
  tabData: TabConfig[] = [
    { id: 'suppliers', label: 'Lieferanten', index: 0 },
    { id: 'costcenters', label: 'Kostenstellen', index: 1 },
    { id: 'projects', label: 'Projekte', index: 2 },
    { id: 'users', label: 'Benutzer & Rollen', index: 3 },
    { id: 'escalation', label: 'Eskalations-Einstellungen', index: 4 }
  ];

  // Table columns
  supplierColumns = ['id', 'name', 'email', 'phone', 'isActive', 'actions'];
  costCenterColumns = ['id', 'name', 'description', 'budget', 'isActive', 'actions'];
  projectColumns = ['id', 'name', 'costCenter', 'budget', 'status', 'actions'];
  purchaseOrderColumns = ['id', 'title', 'totalAmount', 'status', 'createdAt', 'actions'];
  userColumns = ['username', 'fullName', 'email', 'role', 'isActive', 'lastLogin', 'actions'];

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Set active tab based on route parameter
    const activeTabParam = this.route.snapshot.data['activeTab'];
    if (activeTabParam) {
      const tabIndex = this.tabData.find(tab => tab.id === activeTabParam)?.index;
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
    this.loadUsers();
  }

  // Suppliers
  loadSuppliers(): void {
    this.loadingSuppliers = true;
    this.http.get<Supplier[]>(`${this.apiUrl}/suppliers`).subscribe({
      next: (data) => {
        this.suppliers = data;
        this.loadingSuppliers = false;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
        this.loadingSuppliers = false;
        this.snackBar.open('Fehler beim Laden der Lieferanten', 'Schließen', { duration: 3000 });
      }
    });
  }

  deleteSupplier(id: number): void {
    if (confirm('Möchten Sie diesen Lieferanten wirklich löschen?')) {
      this.http.delete(`${this.apiUrl}/suppliers/${id}`).subscribe({
        next: () => {
          this.snackBar.open('Lieferant gelöscht', 'Schließen', { duration: 3000 });
          this.loadSuppliers();
        },
        error: (error) => {
          console.error('Error deleting supplier:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', { duration: 3000 });
        }
      });
    }
  }

  // Cost Centers
  loadCostCenters(): void {
    this.loadingCostCenters = true;
    this.http.get<CostCenter[]>(`${this.apiUrl}/costcenters`).subscribe({
      next: (data) => {
        this.costCenters = data;
        this.loadingCostCenters = false;
      },
      error: (error) => {
        console.error('Error loading cost centers:', error);
        this.loadingCostCenters = false;
        this.snackBar.open('Fehler beim Laden der Kostenstellen', 'Schließen', { duration: 3000 });
      }
    });
  }

  deleteCostCenter(id: string): void {
    if (confirm('Möchten Sie diese Kostenstelle wirklich löschen?')) {
      this.http.delete(`${this.apiUrl}/costcenters/${id}`).subscribe({
        next: () => {
          this.snackBar.open('Kostenstelle gelöscht', 'Schließen', { duration: 3000 });
          this.loadCostCenters();
        },
        error: (error) => {
          console.error('Error deleting cost center:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', { duration: 3000 });
        }
      });
    }
  }

  // Projects
  loadProjects(): void {
    this.loadingProjects = true;
    this.http.get<Project[]>(`${this.apiUrl}/costcenters/all/projects`).subscribe({
      next: (data) => {
        this.projects = data;
        this.loadingProjects = false;
      },
      error: (error) => {
        console.error('Error loading projects:', error);
        this.loadingProjects = false;
        this.snackBar.open('Fehler beim Laden der Projekte', 'Schließen', { duration: 3000 });
      }
    });
  }

  deleteProject(id: string): void {
    if (confirm('Möchten Sie dieses Projekt wirklich löschen?')) {
      this.http.delete(`${this.apiUrl}/projects/${id}`).subscribe({
        next: () => {
          this.snackBar.open('Projekt gelöscht', 'Schließen', { duration: 3000 });
          this.loadProjects();
        },
        error: (error) => {
          console.error('Error deleting project:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', { duration: 3000 });
        }
      });
    }
  }

  // Purchase Orders
  loadPurchaseOrders(): void {
    this.loadingPurchaseOrders = true;
    this.http.get<PurchaseOrder[]>(`${this.apiUrl}/purchaseorders`).subscribe({
      next: (data) => {
        this.purchaseOrders = data;
        this.loadingPurchaseOrders = false;
      },
      error: (error) => {
        console.error('Error loading purchase orders:', error);
        this.loadingPurchaseOrders = false;
        this.snackBar.open('Fehler beim Laden der Bestellungen', 'Schließen', { duration: 3000 });
      }
    });
  }

  deletePurchaseOrder(id: string): void {
    if (confirm('Möchten Sie diese Bestellung wirklich löschen?')) {
      this.http.delete(`${this.apiUrl}/purchaseorders/${id}`).subscribe({
        next: () => {
          this.snackBar.open('Bestellung gelöscht', 'Schließen', { duration: 3000 });
          this.loadPurchaseOrders();
        },
        error: (error) => {
          console.error('Error deleting purchase order:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', { duration: 3000 });
        }
      });
    }
  }

  // Users
  loadUsers(): void {
    this.loadingUsers = true;
    this.http.get<User[]>(`${this.apiUrl}/users`).subscribe({
      next: (data) => {
        this.users = data;
        this.loadingUsers = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.loadingUsers = false;
        this.snackBar.open('Fehler beim Laden der Benutzer', 'Schließen', { duration: 3000 });
      }
    });
  }

  deleteUser(id: string): void {
    if (confirm('Möchten Sie diesen Benutzer wirklich löschen?')) {
      this.http.delete(`${this.apiUrl}/users/${id}`).subscribe({
        next: () => {
          this.snackBar.open('Benutzer gelöscht', 'Schließen', { duration: 3000 });
          this.loadUsers();
        },
        error: (error) => {
          console.error('Error deleting user:', error);
          this.snackBar.open('Fehler beim Löschen', 'Schließen', { duration: 3000 });
        }
      });
    }
  }

  // Create Methods
  createSupplier(): void {
    const dialogData: CreateSupplierData = {
      name: '',
      country: 'Deutschland',
      isActive: true
    };

    const dialogRef = this.dialog.open(CreateSupplierDialogComponent, {
      width: '600px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.post<Supplier>(`${this.apiUrl}/suppliers`, result).subscribe({
          next: () => {
            this.snackBar.open('Lieferant erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadSuppliers();
          },
          error: (error) => {
            console.error('Error creating supplier:', error);
            this.snackBar.open('Fehler beim Erstellen des Lieferanten', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  createCostCenter(): void {
    const dialogData: CreateCostCenterData = {
      id: '',
      name: '',
      budget: 0
    };

    const dialogRef = this.dialog.open(CreateCostCenterDialogComponent, {
      width: '500px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.post<CostCenter>(`${this.apiUrl}/costcenters`, result).subscribe({
          next: () => {
            this.snackBar.open('Kostenstelle erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadCostCenters();
          },
          error: (error) => {
            console.error('Error creating cost center:', error);
            this.snackBar.open('Fehler beim Erstellen der Kostenstelle', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  createProject(): void {
    const dialogData: CreateProjectData = {
      id: '',
      name: '',
      costCenterId: this.costCenters.length > 0 ? this.costCenters[0].id : '',
      budget: 0,
      status: 'Geplant'
    };

    const dialogRef = this.dialog.open(CreateProjectDialogComponent, {
      width: '600px',
      data: {
        project: dialogData,
        costCenters: this.costCenters
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.post<Project>(`${this.apiUrl}/costcenters/${result.costCenterId}/projects`, result).subscribe({
          next: () => {
            this.snackBar.open('Projekt erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadProjects();
          },
          error: (error) => {
            console.error('Error creating project:', error);
            this.snackBar.open('Fehler beim Erstellen des Projekts', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  createUser(): void {
    const dialogData: CreateUserData = {
      username: '',
      email: '',
      firstName: '',
      lastName: '',
      roleIds: []
    };

    const dialogRef = this.dialog.open(CreateUserDialogComponent, {
      width: '500px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.post<User>(`${this.apiUrl}/users`, result).subscribe({
          next: () => {
            this.snackBar.open('Benutzer erfolgreich erstellt', 'Schließen', { duration: 3000 });
            this.loadUsers();
          },
          error: (error) => {
            console.error('Error creating user:', error);
            this.snackBar.open('Fehler beim Erstellen des Benutzers', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  createEscalationRule(): void {
    this.snackBar.open('Eskalations-Regeln werden in Kürze verfügbar sein', 'Schließen', { duration: 3000 });
  }

  // Edit Methods
  editSupplier(supplier: Supplier): void {
    const dialogRef = this.dialog.open(EditSupplierDialogComponent, {
      width: '600px',
      data: supplier
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.put<Supplier>(`${this.apiUrl}/suppliers/${supplier.id}`, result).subscribe({
          next: () => {
            this.snackBar.open('Lieferant erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
            this.loadSuppliers();
          },
          error: (error) => {
            console.error('Error updating supplier:', error);
            this.snackBar.open('Fehler beim Aktualisieren des Lieferanten', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  editCostCenter(costCenter: CostCenter): void {
    const dialogRef = this.dialog.open(EditCostCenterDialogComponent, {
      width: '500px',
      data: costCenter
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.put<CostCenter>(`${this.apiUrl}/costcenters/${costCenter.id}`, result).subscribe({
          next: () => {
            this.snackBar.open('Kostenstelle erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
            this.loadCostCenters();
          },
          error: (error) => {
            console.error('Error updating cost center:', error);
            this.snackBar.open('Fehler beim Aktualisieren der Kostenstelle', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  editProject(project: Project): void {
    const dialogRef = this.dialog.open(EditProjectDialogComponent, {
      width: '600px',
      data: {
        project: project,
        costCenters: this.costCenters
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.put<Project>(`${this.apiUrl}/costcenters/${project.costCenterId}/projects/${project.id}`, result).subscribe({
          next: () => {
            this.snackBar.open('Projekt erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
            this.loadProjects();
          },
          error: (error) => {
            console.error('Error updating project:', error);
            this.snackBar.open('Fehler beim Aktualisieren des Projekts', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  editUser(user: User): void {
    const dialogRef = this.dialog.open(EditUserDialogComponent, {
      width: '500px',
      data: user
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.http.put<User>(`${this.apiUrl}/users/${user.id}`, result).subscribe({
          next: () => {
            this.snackBar.open('Benutzer erfolgreich aktualisiert', 'Schließen', { duration: 3000 });
            this.loadUsers();
          },
          error: (error) => {
            console.error('Error updating user:', error);
            this.snackBar.open('Fehler beim Aktualisieren des Benutzers', 'Schließen', { duration: 3000 });
          }
        });
      }
    });
  }

  // Format currency
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(amount);
  }

  // Format date
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('de-DE');
  }

  // Rollen verwalten
  manageRoles(): void {
    const dialogRef = this.dialog.open(RoleManagementDialogComponent, {
      width: '900px',
      maxWidth: '95vw',
      height: '700px',
      maxHeight: '95vh',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // Wenn Änderungen gespeichert wurden, lade Benutzer neu
        this.loadUsers();
        this.snackBar.open('Rollen-Konfiguration aktualisiert', 'Schließen', { duration: 3000 });
      }
    });
  }
}
