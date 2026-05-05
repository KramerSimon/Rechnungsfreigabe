import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PurchaseOrder } from '../../../../core/models/purchaseOrder.model';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { Project } from '../../../../core/models/project.model';
import { PurchaseOrderService } from '../../../../core/services/purchase-order.service';
import { CostCenterService } from '../../../../core/services/cost-center.service';
import { ProjectService } from '../../../../core/services/project.service';
import { CreatePurchaseOrderDialogComponent } from './dialogs/create-purchase-order-dialog.component';
import { forkJoin } from 'rxjs';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-purchase-orders-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatTooltipModule,
  ],
  templateUrl: './purchase-orders-tab.component.html',
  styleUrls: ['./purchase-orders-tab.component.scss'],
})
export class PurchaseOrdersTabComponent implements OnInit {
  purchaseOrders: PurchaseOrder[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];
  loadingPurchaseOrders = false;
  purchaseOrderColumns = ['id', 'title', 'costCenter', 'project', 'totalAmount', 'status', 'createdAt', 'actions'];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private purchaseOrderService: PurchaseOrderService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadPurchaseOrders();
  }

  loadPurchaseOrders(): void {
    this.loadingPurchaseOrders = true;
    forkJoin({
      purchaseOrders: this.purchaseOrderService.getPurchaseOrders(),
      costCenters: this.costCenterService.getCostCenters(),
      projects: this.projectService.getProjects()
    }).subscribe({
      next: (result) => {
        this.purchaseOrders = result.purchaseOrders;
        this.costCenters = result.costCenters;
        this.projects = result.projects;
        this.loadingPurchaseOrders = false;
      },
      error: (error: any) => {
        console.error('Error loading purchase orders:', error);
        this.loadingPurchaseOrders = false;
        this.snackBar.open(this.t('md.purchaseOrders.error.load'), this.t('common.close'), {
          duration: 3000,
        });
      },
    });
  }

  createPurchaseOrder(): void {
    const dialogData = {
      costCenters: this.costCenters,
      projects: this.projects
    };

    const dialogRef = this.dialog.open(CreatePurchaseOrderDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.purchaseOrderService.createPurchaseOrder(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.purchaseOrders.success.created'), this.t('common.close'), {
              duration: 3000,
            });
            this.loadPurchaseOrders();
          },
          error: (error: any) => {
            console.error('Error creating purchase order:', error);
            this.snackBar.open(
              this.t('md.purchaseOrders.error.create'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  deletePurchaseOrder(id: string): void {
    if (confirm(this.t('md.purchaseOrders.confirm.delete'))) {
      this.purchaseOrderService.deletePurchaseOrder(id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.purchaseOrders.success.deleted'), this.t('common.close'), {
            duration: 3000,
          });
          this.loadPurchaseOrders();
        },
        error: (error: any) => {
          console.error('Error deleting purchase order:', error);
          this.snackBar.open(
            this.t('md.purchaseOrders.error.delete'),
            this.t('common.close'),
            { duration: 3000 }
          );
        },
      });
    }
  }

  getCostCenterName(costCenterId: string): string {
    const costCenter = this.costCenters.find(cc => cc.id === String(costCenterId));
    return costCenter ? costCenter.name : '-';
  }

  getProjectName(projectId: string): string {
    const project = this.projects.find(p => p.id === String(projectId));
    return project ? project.name : '-';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount);
  }

  formatDate(date: string | Date): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('de-DE');
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
