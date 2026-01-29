import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute } from '@angular/router';
import { TabConfig } from '../../core/interfaces/common.interfaces';
import { SuppliersTabComponent } from './tabs/suppliers-tab/suppliers-tab.component';
import { CostCentersTabComponent } from './tabs/cost-centers-tab/cost-centers-tab.component';
import { ProjectsTabComponent } from './tabs/projects-tab/projects-tab.component';
import { PurchaseOrdersTabComponent } from './tabs/purchase-orders-tab/purchase-orders-tab.component';
import { InvoicesTabComponent } from './tabs/invoices-tab/invoices-tab.component';
import { UsersTabComponent } from './tabs/users-tab/users-tab.component';
import { RolesTabComponent } from './tabs/roles-tab/roles-tab.component';
import { EscalationTabComponent } from './tabs/escalation-tab/escalation-tab.component';
import { ApprovalRulesTabComponent } from './tabs/approval-rules-tab/approval-rules-tab.component';
import { ApprovalWorkflowsTabComponent } from './tabs/approval-workflows-tab/approval-workflows-tab.component';

@Component({
  selector: 'app-master-data',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    SuppliersTabComponent,
    CostCentersTabComponent,
    ProjectsTabComponent,
    PurchaseOrdersTabComponent,
    InvoicesTabComponent,
    UsersTabComponent,
    RolesTabComponent,
    EscalationTabComponent,
    ApprovalRulesTabComponent,
    ApprovalWorkflowsTabComponent,
  ],
  templateUrl: './master-data.component.html',
  styleUrls: ['./master-data.component.scss'],
})
export class MasterDataComponent implements OnInit {
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

  constructor(private route: ActivatedRoute) {}

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
  }
}

