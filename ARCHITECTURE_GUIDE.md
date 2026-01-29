# Master-Data Component Architecture

## Component Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│               MasterDataComponent (Container)                │
│                     (69 lines TS)                            │
│                     (56 lines HTML)                          │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              <mat-tab-group>                          │  │
│  │                                                        │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │ <mat-tab> "Lieferanten"                          │ │  │
│  │  │   <app-suppliers-tab></app-suppliers-tab>       │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │                                                        │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │ <mat-tab> "Kostenstellen"                        │ │  │
│  │  │   <app-cost-centers-tab></app-cost-centers-tab>│ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │                                                        │  │
│  │  ┌─────────────────────────────────────────────────┐ │  │
│  │  │ <mat-tab> "Projekte"                            │ │  │
│  │  │   <app-projects-tab></app-projects-tab>        │ │  │
│  │  └─────────────────────────────────────────────────┘ │  │
│  │                                                        │  │
│  │  ... [7 more tabs] ...                              │  │
│  │                                                        │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Tab Components (Detailed)

Each tab component follows this structure:

```
suppliers-tab/
├── suppliers-tab.component.ts     [Logic, CRUD operations]
├── suppliers-tab.component.html   [Table, buttons, dialogs]
├── suppliers-tab.component.scss   [Styles]
└── dialogs/
    ├── create-supplier-dialog/
    │   ├── create-supplier-dialog.component.ts
    │   ├── create-supplier-dialog.component.html
    │   └── create-supplier-dialog.component.scss
    └── edit-supplier-dialog/
        ├── edit-supplier-dialog.component.ts
        ├── edit-supplier-dialog.component.html
        └── edit-supplier-dialog.component.scss
```

## Data Flow

### Route Navigation
```
User clicks link
   ↓
Routes to /admin/suppliers (with data: { activeTab: 'suppliers' })
   ↓
MasterDataComponent initializes
   ↓
Reads activeTab from route.snapshot.data
   ↓
Sets activeTab index to 0 (suppliers)
   ↓
Template renders <app-suppliers-tab>
   ↓
SuppliersTabComponent loads and displays suppliers
```

### Tab Interaction
```
User in SuppliersTab
   ↓
Clicks "Create Supplier" button
   ↓
SuppliersTabComponent.createSupplier()
   ↓
Opens CreateSupplierDialogComponent
   ↓
User fills form and submits
   ↓
Dialog returns data via afterClosed()
   ↓
SuppliersTabComponent calls supplierService.createSupplier()
   ↓
Service makes API call
   ↓
Component shows snackbar notification
   ↓
Component reloads suppliers list
```

## All Tab Components

| # | Component | Route | Features |
|---|-----------|-------|----------|
| 1 | **Suppliers Tab** | `/admin/suppliers` | Create, read, update, delete suppliers |
| 2 | **Cost Centers Tab** | `/admin/cost-centers` | Manage cost centers with budgets, manager assignment |
| 3 | **Projects Tab** | `/admin/projects` | Create projects, link to cost centers, status tracking |
| 4 | **Purchase Orders Tab** | `/admin/purchase-orders` | Create purchase orders, currency formatting |
| 5 | **Invoices Tab** | `/admin/invoices` | Read-only invoices, links to detail pages |
| 6 | **Users Tab** | `/admin/users` | Manage users, role assignment, last login tracking |
| 7 | **Roles Tab** | `/admin/roles` | Create/manage roles, permissions, system role protection |
| 8 | **Escalation Tab** | `/admin/escalation` | Create escalation rules, time-based triggers |
| 9 | **Approval Rules Tab** | `/admin/rules` | Create approval rules with conditions/actions |
| 10 | **Approval Workflows Tab** | `/admin/workflows` | Manage approval workflows for invoices |

## Services Used (per tab)

### Suppliers Tab
- `SupplierService`

### Cost Centers Tab
- `CostCenterService`
- `UserService` (for manager list)

### Projects Tab
- `ProjectService`
- `CostCenterService`
- `UserService`

### Purchase Orders Tab
- `PurchaseOrderService`
- `CostCenterService`
- `ProjectService`

### Invoices Tab
- `InvoiceService`
- `SupplierService`
- `StatusService`

### Users Tab
- `UserService`
- `RoleService`

### Roles Tab
- `RoleService`

### Escalation Tab
- `EscalationRuleService`
- `StatusService` (for display)

### Approval Rules Tab
- `ApprovalService`

### Approval Workflows Tab
- `ApprovalService`

## Master Data Container Logic

The main component is extremely simple:

```typescript
export class MasterDataComponent implements OnInit {
  activeTab = 0;
  
  tabData: TabConfig[] = [
    { id: 'suppliers', label: 'Lieferanten', index: 0 },
    // ... 9 more tabs ...
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
```

**That's it!** All the business logic has been moved to individual tab components.

## Master Data Template

```html
<div class="master-data-container">
  <h1>Stammdatenverwaltung</h1>

  <mat-tab-group [(selectedIndex)]="activeTab">
    <mat-tab label="Lieferanten">
      <app-suppliers-tab></app-suppliers-tab>
    </mat-tab>
    
    <mat-tab label="Kostenstellen">
      <app-cost-centers-tab></app-cost-centers-tab>
    </mat-tab>
    
    <!-- ... 8 more tabs ... -->
    
    <mat-tab label="Genehmigungsworkflows">
      <app-approval-workflows-tab></app-approval-workflows-tab>
    </mat-tab>
  </mat-tab-group>
</div>
```

**Clean. Simple. Maintainable.**

## Refactoring Results

### Before
- **1 monolithic component** with 1,331 lines
- **Scattered dialog folder** with 15+ dialog components
- **Difficult to maintain** - changes affect entire file
- **Hard to test** - all logic interdependent
- **Cannot reuse tabs** - too tightly coupled

### After
- **10 modular components**, each focused on one task
- **Organized dialogs** - co-located with their tabs
- **Easy to maintain** - changes isolated to single tab
- **Easy to test** - each tab testable independently
- **Highly reusable** - tabs can be used in other contexts

## Routing Benefits

All existing routes continue to work seamlessly:

```
GET /admin/suppliers → Master Data loads with Suppliers Tab active
GET /admin/cost-centers → Master Data loads with Cost Centers Tab active
GET /admin/projects → Master Data loads with Projects Tab active
... and so on for all 10 tabs ...
```

No routing changes needed. The `activeTab` route data parameter handles everything.

## Future Improvements

This architecture enables:

1. **Lazy Loading** - Load tabs on demand for better performance
2. **Tab-level Services** - Inject services at tab component level
3. **Shared State** - Use a state management library for cross-tab communication
4. **Tab Guards** - Add permission guards at tab level
5. **Tab History** - Track which tabs were visited
6. **Tab Analytics** - Track user behavior per tab

## Summary

The refactoring successfully transformed a monolithic component into a clean, modular architecture while maintaining:
- ✅ 100% routing compatibility
- ✅ All existing functionality
- ✅ Identical user experience
- ✅ Improved code organization
- ✅ Better maintainability

The codebase is now ready for future enhancements and easier to maintain and test!
