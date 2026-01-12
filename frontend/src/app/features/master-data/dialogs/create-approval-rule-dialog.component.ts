import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CreateApprovalRuleDto } from '../../../core/models/approval.model';
import { SupplierService } from '../../../core/services/supplier.service';
import { CostCenterService } from '../../../core/services/cost-center.service';
import { ProjectService } from '../../../core/services/project.service';
import { Supplier } from '../../../core/models/supplier.model';
import { CostCenter } from '../../../core/models/cost-center.model';
import { Project } from '../../../core/models/project.model';

@Component({
  selector: 'app-create-approval-rule-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatCheckboxModule,
    ReactiveFormsModule
  ],
  template: `
    <div class="dialog-container">
      <h2 mat-dialog-title>Neue Genehmigungsregel</h2>

      <form [formGroup]="ruleForm" mat-dialog-content>
        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Name *</mat-label>
            <input matInput formControlName="name" required>
            <mat-error *ngIf="ruleForm.get('name')?.hasError('required')">
              Name ist erforderlich
            </mat-error>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Beschreibung</mat-label>
            <textarea matInput formControlName="description" rows="3"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Regeltyp *</mat-label>
            <mat-select formControlName="ruleType" required>
              <mat-option value="Manual">Manuell</mat-option>
              <mat-option value="Automatic">Automatisch</mat-option>
              <mat-option value="Conditional">Bedingt</mat-option>
            </mat-select>
            <mat-error *ngIf="ruleForm.get('ruleType')?.hasError('required')">
              Regeltyp ist erforderlich
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Priorität</mat-label>
            <input matInput type="number" formControlName="priority" min="1" max="100">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline">
            <mat-label>Lieferant</mat-label>
            <mat-select formControlName="supplierId">
              <mat-option [value]="null">-- Keiner --</mat-option>
              <mat-option *ngFor="let supplier of suppliers" [value]="supplier.id">
                {{ supplier.name }}
              </mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Kostenstelle</mat-label>
            <mat-select formControlName="costCenterId">
              <mat-option [value]="null">-- Keine --</mat-option>
              <mat-option *ngFor="let costCenter of costCenters" [value]="costCenter.id">
                {{ costCenter.name }}
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Projekt</mat-label>
            <mat-select formControlName="projectId">
              <mat-option [value]="null">-- Keines --</mat-option>
              <mat-option *ngFor="let project of projects" [value]="project.id">
                {{ project.name }}
              </mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </form>

      <div mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">Abbrechen</button>
        <button mat-raised-button color="primary"
                [disabled]="!ruleForm.valid"
                (click)="onSave()">
          Speichern
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      padding: 20px;
      min-width: 400px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }

    mat-form-field {
      flex: 1;
    }

    .full-width {
      width: 100%;
    }

    [mat-dialog-actions] {
      margin-top: 24px;
    }
  `]
})
export class CreateApprovalRuleDialogComponent implements OnInit {
  ruleForm: FormGroup;
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<CreateApprovalRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateApprovalRuleDto,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService
  ) {
    this.ruleForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      ruleType: ['Manual', Validators.required],
      priority: [10],
      supplierId: [null],
      costCenterId: [null],
      projectId: [null],
    });
  }

  ngOnInit(): void {
    this.loadSuppliers();
    this.loadCostCenters();
    this.loadProjects();
  }

  loadSuppliers(): void {
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
      },
      error: (error) => {
        console.error('Fehler beim Laden der Lieferanten:', error);
      }
    });
  }

  loadCostCenters(): void {
    this.costCenterService.getCostCenters().subscribe({
      next: (costCenters) => {
        this.costCenters = costCenters;
      },
      error: (error) => {
        console.error('Fehler beim Laden der Kostenstellen:', error);
      }
    });
  }

  loadProjects(): void {
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
      },
      error: (error) => {
        console.error('Fehler beim Laden der Projekte:', error);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.ruleForm.valid) {
      this.dialogRef.close(this.ruleForm.value);
    }
  }
}
