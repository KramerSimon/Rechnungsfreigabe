import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { User } from '../../../../core/models/user.models';
import { CostCenterService } from '../../../../core/services/cost-center.service';
import { UserService } from '../../../../core/services/user.service';
import { CreateCostCenterDialogComponent } from './dialogs/create-cost-center-dialog.component';
import { EditCostCenterDialogComponent } from './dialogs/edit-cost-center-dialog/edit-cost-center-dialog.component';
import { forkJoin } from 'rxjs';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-cost-centers-tab',
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
  templateUrl: './cost-centers-tab.component.html',
  styleUrls: ['./cost-centers-tab.component.scss'],
})
export class CostCentersTabComponent implements OnInit {
  costCenters: CostCenter[] = [];
  users: User[] = [];
  loadingCostCenters = false;
  costCenterColumns = ['id', 'name', 'description', 'budget', 'manager', 'isActive', 'actions'];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private costCenterService: CostCenterService,
    private userService: UserService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadCostCenters();
  }

  loadCostCenters(): void {
    this.loadingCostCenters = true;
    forkJoin({
      costCenters: this.costCenterService.getCostCenters(),
      users: this.userService.getUsers()
    }).subscribe({
      next: (result) => {
        this.costCenters = result.costCenters;
        this.users = result.users;
        this.loadingCostCenters = false;
      },
      error: (error: any) => {
        console.error('Error loading cost centers:', error);
        this.loadingCostCenters = false;
        this.snackBar.open(this.t('md.costCenters.error.load'), this.t('common.close'), {
          duration: 3000,
        });
      },
    });
  }

  createCostCenter(): void {
    const dialogData = {
      costCenter: {
        id: '',
        name: '',
        description: '',
        budget: 0,
        managerId: '',
        isActive: true,
      } as CostCenter,
      managers: this.users.filter(u => this.isUserAManager(u))
    };

    const dialogRef = this.dialog.open(CreateCostCenterDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.costCenterService.addCostCenter(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.costCenters.success.created'), this.t('common.close'), {
              duration: 3000,
            });
            this.loadCostCenters();
          },
          error: (error: any) => {
            console.error('Error creating cost center:', error);
            this.snackBar.open(
              this.t('md.costCenters.error.create'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editCostCenter(costCenter: CostCenter): void {
    const dialogData = {
      costCenter: costCenter,
      managers: this.users.filter(u => this.isUserAManager(u))
    };

    const dialogRef = this.dialog.open(EditCostCenterDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.costCenterService.updateCostCenter(costCenter.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              this.t('md.costCenters.success.updated'),
              this.t('common.close'),
              { duration: 3000 }
            );
            this.loadCostCenters();
          },
          error: (error: any) => {
            console.error('Error updating cost center:', error);
            this.snackBar.open(
              this.t('md.costCenters.error.update'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  deleteCostCenter(id: string): void {
    if (confirm(this.t('md.costCenters.confirm.delete'))) {
      this.costCenterService.deleteCostCenter(id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.costCenters.success.deleted'), this.t('common.close'), {
            duration: 3000,
          });
          this.loadCostCenters();
        },
        error: (error: any) => {
          console.error('Error deleting cost center:', error);
          this.snackBar.open(
            this.t('md.costCenters.error.delete'),
            this.t('common.close'),
            { duration: 3000 }
          );
        },
      });
    }
  }

  getManagerName(costCenter: CostCenter): string {
    if (costCenter.manager) {
      const fullName = [costCenter.manager.firstName, costCenter.manager.lastName]
        .filter(Boolean)
        .join(' ');
      return fullName || costCenter.manager.username;
    }
    return '-';
  }

  isUserAManager(user: User): boolean {
    const role = user.role?.toLowerCase();
    return role === 'manager' || role === 'admin' || role === 'administrator';
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
