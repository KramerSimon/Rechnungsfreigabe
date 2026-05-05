import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Supplier } from '../../../../core/models/supplier.model';
import { SupplierService } from '../../../../core/services/supplier.service';
import { CreateSupplierDialogComponent } from './dialogs/create-supplier-dialog.component';
import { EditSupplierDialogComponent } from './dialogs/edit-supplier-dialog/edit-supplier-dialog.component';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-suppliers-tab',
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
  templateUrl: './suppliers-tab.component.html',
  styleUrls: ['./suppliers-tab.component.scss'],
})
export class SuppliersTabComponent implements OnInit {
  suppliers: Supplier[] = [];
  loadingSuppliers = false;
  supplierColumns = ['id', 'name', 'email', 'phone', 'isActive', 'actions'];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private supplierService: SupplierService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.loadingSuppliers = true;
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
        this.loadingSuppliers = false;
      },
      error: (error: any) => {
        console.error('Error loading suppliers:', error);
        this.loadingSuppliers = false;
        this.snackBar.open(this.t('md.suppliers.error.load'), this.t('common.close'), {
          duration: 3000,
        });
      },
    });
  }

  createSupplier(): void {
    const dialogData: Supplier = {
      id: 0,
      name: '',
    };

    const dialogRef = this.dialog.open(CreateSupplierDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.supplierService.createSupplier(result).subscribe({
          next: () => {
            this.snackBar.open(this.t('md.suppliers.success.created'), this.t('common.close'), {
              duration: 3000,
            });
            this.loadSuppliers();
          },
          error: (error: any) => {
            console.error('Error creating supplier:', error);
            this.snackBar.open(
              this.t('md.suppliers.error.create'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editSupplier(supplier: Supplier): void {
    const dialogRef = this.dialog.open(EditSupplierDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: supplier,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.supplierService.updateSupplier(supplier.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              this.t('md.suppliers.success.updated'),
              this.t('common.close'),
              { duration: 3000 }
            );
            this.loadSuppliers();
          },
          error: (error: any) => {
            console.error('Error updating supplier:', error);
            this.snackBar.open(
              this.t('md.suppliers.error.update'),
              this.t('common.close'),
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  deleteSupplier(id: number): void {
    if (confirm(this.t('md.suppliers.confirm.delete'))) {
      this.supplierService.deleteSupplier(id).subscribe({
        next: () => {
          this.snackBar.open(this.t('md.suppliers.success.deleted'), this.t('common.close'), {
            duration: 3000,
          });
          this.loadSuppliers();
        },
        error: (error: any) => {
          console.error('Error deleting supplier:', error);
          this.snackBar.open(
            this.t('md.suppliers.error.delete'),
            this.t('common.close'),
            { duration: 3000 }
          );
        },
      });
    }
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
