import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ApprovalRule } from '../../../../../../core/models/approval.model';
import { SupplierService } from '../../../../../../core/services/supplier.service';
import { CostCenterService } from '../../../../../../core/services/cost-center.service';
import { ProjectService } from '../../../../../../core/services/project.service';
import { Supplier } from '../../../../../../core/models/supplier.model';
import { CostCenter } from '../../../../../../core/models/cost-center.model';
import { Project } from '../../../../../../core/models/project.model';

@Component({
  selector: 'app-edit-approval-rule-dialog',
  templateUrl: './edit-approval-rule-dialog.component.html',
  styleUrls: ['./edit-approval-rule-dialog.component.scss'],
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
  ]
})
export class EditApprovalRuleDialogComponent implements OnInit {
  ruleForm: FormGroup;
  suppliers: Supplier[] = [];
  costCenters: CostCenter[] = [];
  projects: Project[] = [];

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<EditApprovalRuleDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ApprovalRule,
    private supplierService: SupplierService,
    private costCenterService: CostCenterService,
    private projectService: ProjectService
  ) {
    this.ruleForm = this.fb.group({
      name: [data.name, Validators.required],
      description: [data.description || ''],
      ruleType: [data.ruleType, Validators.required],
      priority: [data.priority],
      isActive: [data.isActive],
      supplierId: [data.supplierId || null],
      costCenterId: [data.costCenterId || null],
      projectId: [data.projectId || null],
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
