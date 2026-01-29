# Service Refactoring Guide - Removing .Query() Calls

## ✅ Completed Services

### UserService.cs
- ✅ GetAllUsersAsync() → Users.GetAllActiveWithRolesAsync()
- ✅ GetUserByIdAsync() → Users.GetByIdWithRolesAsync()
- ✅ GetUserByUsernameAsync() → Users.GetByUsernameWithFullDetailsAsync()
- ✅ GetUserEntityByIdAsync() → Users.GetByIdWithFullDetailsAsync()
- ✅ CreateUserAsync() reload → Users.GetByIdWithRolesAsync()
- ✅ UpdateUserAsync() load & reload → Users.GetByIdWithRolesAsync()
- ✅ GetUserPermissionsAsync() → UserRoles.GetUserPermissionsAsync()
- ⚠️ GetUsersPagedAsync() still uses Query() - needs special handling for complex filtering/sorting

**Errors Fixed:** 8/9 Query() calls

---

## 🔴 Services Requiring Refactoring

### Strategy for Each Service:
1. Read the service file to identify all `.Query()` patterns
2. Check if corresponding repository method exists
3. If YES → Replace `.Query()` with repository method
4. If NO → Add new method to repository interface and implementation
5. Test compilation after each change
6. Commit when service compiles successfully

---

## 📋 Refactoring Patterns

### Pattern 1: Simple Query with Include
**Before:**
```csharp
var entity = await _unitOfWork.Entities.Query()
    .Include(e => e.Related)
    .FirstOrDefaultAsync(e => e.Id == id);
```

**After:**
```csharp
// Add to IEntityRepository:
Task<Entity?> GetByIdWithRelatedAsync(int id);

// Implementation:
public async Task<Entity?> GetByIdWithRelatedAsync(int id)
{
    return await _dbSet
        .Include(e => e.Related)
        .FirstOrDefaultAsync(e => e.Id == id);
}

// Service usage:
var entity = await _unitOfWork.Entities.GetByIdWithRelatedAsync(id);
```

### Pattern 2: Where + ToListAsync
**Before:**
```csharp
var entities = await _unitOfWork.Entities.Query()
    .Where(e => e.IsActive)
    .ToListAsync();
```

**After:**
```csharp
// Add to IEntityRepository:
Task<IEnumerable<Entity>> GetActiveAsync();

// Implementation:
public async Task<IEnumerable<Entity>> GetActiveAsync()
{
    return await _dbSet
        .Where(e => e.IsActive)
        .ToListAsync();
}

// Service usage:
var entities = await _unitOfWork.Entities.GetActiveAsync();
```

### Pattern 3: Existence Check (AnyAsync)
**Before:**
```csharp
var exists = await _unitOfWork.Entities.Query()
    .AnyAsync(e => e.Code == code);
```

**After:**
```csharp
// Add to IEntityRepository:
Task<bool> ExistsByCodeAsync(string code);

// Implementation:
public async Task<bool> ExistsByCodeAsync(string code)
{
    return await _dbSet.AnyAsync(e => e.Code == code);
}

// Service usage:
var exists = await _unitOfWork.Entities.ExistsByCodeAsync(code);
```

### Pattern 4: Multiple Includes (Deep Loading)
**Before:**
```csharp
var entity = await _unitOfWork.Entities.Query()
    .Include(e => e.Child)
    .ThenInclude(c => c.GrandChild)
    .FirstOrDefaultAsync(e => e.Id == id);
```

**After:**
```csharp
// Add to IEntityRepository:
Task<Entity?> GetByIdWithFullDetailsAsync(int id);

// Implementation:
public async Task<Entity?> GetByIdWithFullDetailsAsync(int id)
{
    return await _dbSet
        .Include(e => e.Child)
        .ThenInclude(c => c.GrandChild)
        .FirstOrDefaultAsync(e => e.Id == id);
}

// Service usage:
var entity = await _unitOfWork.Entities.GetByIdWithFullDetailsAsync(id);
```

---

## 🎯 Priority Service List (Ordered by Complexity)

### HIGH PRIORITY - Small Services (Quick Wins)

#### 1. PermissionService.cs (~1 Query call)
- GetAllOrderedAsync() → Use Permissions.GetAllOrderedAsync() ✅ (already exists)

#### 2. SupplierService.cs (~2 Query calls)
**Needed Methods:**
- Suppliers.GetByNameAsync(name) ✅ (already exists)
- Suppliers.GetActiveAsync() ✅ (already exists)

**Actions:**
- Line 19: Replace `.Query().Where(s => s.Name == name)` → `GetByNameAsync(name)`
- Line 127: Replace `.Query().Where(s => s.IsActive).OrderBy()` → `GetActiveAsync()`

#### 3. SystemConfigService.cs (~5 Query calls)
**Needed Methods:**
- SystemConfigs.GetByKeyAsync(key) ✅ (already exists)
- SystemConfigs.GetByCategoryAsync(category) ✅ (already exists)
- SystemConfigs.GetAllAsync() (use base method)

**Actions:**
- Replace all `.Query().FirstOrDefaultAsync(sc => sc.ConfigKey == key)` → `GetByKeyAsync(key)`
- Replace `.Query().Where(sc => sc.Category == category)` → `GetByCategoryAsync(category)`

### MEDIUM PRIORITY - Medium Services

#### 4. RoleService.cs (~7 Query calls)
**Needed Methods:**
- Roles.GetAllWithPermissionsAsync() ✅ (already exists)
- Roles.GetByIdWithPermissionsAsync(id)
- Roles.ExistsByNameAsync(name, excludeId)

**Add to IRoleRepository:**
```csharp
Task<Role?> GetByIdWithPermissionsAsync(int id);
Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
```

**Implementation:**
```csharp
public async Task<Role?> GetByIdWithPermissionsAsync(int id)
{
    return await _dbSet
        .Include(r => r.RolePermissions)
        .ThenInclude(rp => rp.Permission)
        .FirstOrDefaultAsync(r => r.Id == id);
}

public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
{
    var query = _dbSet.Where(r => r.Name == name);
    if (excludeId.HasValue)
        query = query.Where(r => r.Id != excludeId.Value);
    return await query.AnyAsync();
}
```

#### 5. PurchaseOrderService.cs (~4 Query calls)
**Needed Methods:**
- PurchaseOrders.GetByNumberAsync(poNumber) ✅ (already exists)
- PurchaseOrders.GetAllAsync() (use base method)
- PurchaseOrders.GetByProjectIdAsync(projectId) ✅ (already exists)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)

#### 6. ProjectService.cs (~5 Query calls)
**Needed Methods:**
- Projects.GetByIdWithManagerAsync(id) ✅ (already exists)
- Projects.GetByCostCenterAsync(costCenterId) ✅ (already exists)
- Projects.GetAllAsync() (use base method)
- CostCenters.GetByIdWithManagerAsync(id) ✅ (already exists)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)

#### 7. InvoiceHistoryService.cs (~3 Query calls)
**Needed Methods:**
- InvoiceHistories.GetByInvoiceIdAsync(invoiceId) ✅ (already exists)
- Invoices.GetByIdAsync(id) (use base method)
- Users.GetByIdAsync(id) (use base method)

### HIGH EFFORT - Large Services

#### 8. CostCenterService.cs (~10 Query calls)
**Needed Methods:**
- CostCenters.GetAllAsync(), GetByIdWithManagerAsync() ✅ (exist)
- CostCenters.HasRelatedInvoicesAsync(id) ✅ (already exists)
- CostCenters.HasRelatedProjectsAsync(id) ✅ (already exists)
- Invoices.GetByCostCenterIdAsync(id)
- Projects.GetByCostCenterAsync(id) ✅ (already exists)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)

**Add to IInvoiceRepository:**
```csharp
Task<IEnumerable<Invoice>> GetByCostCenterIdAsync(string costCenterId);
```

#### 9. NotificationService.cs (~20 Query calls)
**Needed Methods:**
- Notifications.ExistsWithConditionsAsync() ✅ (already exists)
- Notifications.GetUserNotificationsAsync(userId, unreadOnly) ✅ (already exists)
- Notifications.GetByIdWithIncludesAsync(id) ✅ (already exists)
- Invoices.GetByIdAsync(id) (use base method)
- Users.GetByIdAsync(id) (use base method)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)

**Additional methods needed:**
```csharp
Task<IEnumerable<Notification>> GetAllAsync(); // use base
Task<IEnumerable<Notification>> GetPendingNotificationsAsync();
Task<int> GetUnreadCountAsync(int userId);
```

#### 10. EscalationRuleService.cs (~10 Query calls)
**Needed Methods:**
- EscalationRules.GetActiveRulesAsync() ✅ (already exists)
- EscalationRules.GetByIdWithIncludesAsync(id) ✅ (already exists)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)
- Roles.GetByIdAsync(id) (use base method)
- Users.GetByIdAsync(id) (use base method)

#### 11. EscalationEmailService.cs (~15 Query calls)
**Needed Methods:**
- Invoices.GetWithRelationsAsync(predicate)
- EscalationRules.GetActiveRulesAsync() ✅ (already exists)
- EscalationRules.GetByIdWithIncludesAsync(id) ✅ (already exists)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)
- EscalationLogs.HasEscalationLogAsync(invoiceId, ruleId) ✅ (already exists)
- EscalationLogs.GetByInvoiceAndRuleAsync(invoiceId, ruleId) ✅ (already exists)
- Users.GetByIdAsync(id) (use base method)

**Add to IInvoiceRepository:**
```csharp
Task<IEnumerable<Invoice>> GetEscalationCandidatesAsync(string statusCode, int minutesThreshold);
```

#### 12. PdfUploadService.cs (~15 Query calls)
**Needed Methods:**
- Users.GetByIdAsync(id) (use base method)
- PurchaseOrders.GetByNumberAsync(poNumber) ✅ (already exists)
- CostCenters.GetByIdAsync(id) (use base method)
- Projects.GetByIdAsync(id) (use base method)
- Statuses.GetByCodeAndTypeAsync(code, type) ✅ (already exists)
- Invoices.GetByNumberAsync(number)
- Suppliers.GetByNameAsync(name) ✅ (already exists)

**Add to IInvoiceRepository:**
```csharp
Task<Invoice?> GetByNumberAsync(string invoiceNumber);
Task<bool> ExistsByNumberAsync(string invoiceNumber);
```

### CRITICAL - Largest Services (Most Work)

#### 13. InvoiceService.cs (~50 Query calls)
This service has extensive Query() usage. Will need many repository methods:
- Invoices.GetByIdWithIncludesAsync() ✅
- Invoices.GetByStatusAsync() ✅
- Invoices.GetAllWithRelationsAsync()
- Invoices.GetFilteredAsync(filters, includes)
- Invoices.GetDashboardStatsAsync()
- Statuses.GetByCodeAndTypeAsync() ✅
- ApprovalWorkflows.GetByInvoiceIdAsync()
- CostCenters.GetByIdAsync()

**Estimated Effort:** 4-6 hours

#### 14. ApprovalService.cs (~80 Query calls)
The most complex service with the most Query() calls:
- Invoices.GetByIdWithIncludesAsync() ✅
- ApprovalRules.GetActiveRulesAsync() ✅
- ApprovalRules.GetByPriorityAsync()
- ApprovalWorkflows.GetByIdWithIncludesAsync() ✅
- ApprovalWorkflows.GetByInvoiceIdAsync() ✅
- ApprovalWorkflows.GetPendingForUserAsync() ✅
- ApprovalWorkflows.GetByStatusAsync() ✅
- Statuses.GetByCodeAndTypeAsync() ✅
- Users.GetActiveByIdAsync() ✅
- Users.GetAdminUserAsync() ✅

**Estimated Effort:** 8-10 hours

---

## 🔧 Systematic Refactoring Process

### Step-by-Step for Each Service:

1. **Read Service File**
   ```bash
   cat Interfaces/Services/Implementations/[ServiceName].cs
   ```

2. **Identify All .Query() Calls**
   ```bash
   grep -n "\.Query()" Interfaces/Services/Implementations/[ServiceName].cs
   ```

3. **For Each .Query() Call:**
   - Determine what the query does (filtering, includes, projections)
   - Check if repository method exists in corresponding I[Entity]Repository
   - If NOT exists:
     * Add method signature to interface
     * Implement method in repository class
     * Update IUnitOfWork if new repository type
     * Update UnitOfWork implementation

4. **Replace .Query() in Service**
   - Update service code to use repository method
   - Remove any Include(), Where(), FirstOrDefaultAsync() chains
   - Replace with single repository method call

5. **Build and Test**
   ```bash
   dotnet build
   ```

6. **Verify No Errors for That Service**
   ```bash
   dotnet build 2>&1 | grep "[ServiceName].cs" | grep "error CS"
   ```

7. **Commit Changes**
   ```bash
   git add .
   git commit -m "Refactor [ServiceName] to use repository methods"
   ```

---

## 📊 Progress Tracker

| Service | Query Calls | Status | Priority | Estimated Time |
|---------|------------|--------|----------|----------------|
| UserService | 9 | ✅ 8/9 Done | HIGH | 30min ✅ |
| PermissionService | 1 | ❌ Not Started | HIGH | 10min |
| SupplierService | 2 | ❌ Not Started | HIGH | 15min |
| SystemConfigService | 5 | ❌ Not Started | HIGH | 30min |
| RoleService | 7 | ❌ Not Started | MEDIUM | 45min |
| PurchaseOrderService | 4 | ❌ Not Started | MEDIUM | 30min |
| ProjectService | 5 | ❌ Not Started | MEDIUM | 30min |
| InvoiceHistoryService | 3 | ❌ Not Started | MEDIUM | 20min |
| CostCenterService | 10 | ❌ Not Started | HIGH | 1.5hr |
| NotificationService | 20 | ❌ Not Started | HIGH | 2hr |
| EscalationRuleService | 10 | ❌ Not Started | HIGH | 1.5hr |
| EscalationEmailService | 15 | ❌ Not Started | HIGH | 2hr |
| PdfUploadService | 15 | ❌ Not Started | HIGH | 2hr |
| InvoiceService | 50 | ❌ Not Started | CRITICAL | 4-6hr |
| ApprovalService | 80 | ❌ Not Started | CRITICAL | 8-10hr |

**Total Estimated Effort:** 25-30 hours
**Completed:** ~30 minutes (UserService)
**Remaining:** ~24-30 hours

---

## 💡 Tips for Efficient Refactoring

1. **Start with Smallest Services** - Build confidence and establish patterns
2. **Use multi_replace_string_in_file** - Make multiple changes at once
3. **Group Similar Changes** - Refactor all GetById patterns together
4. **Test Frequently** - Build after each service to catch errors early
5. **Commit Often** - One commit per service for easy rollback
6. **Add Repository Methods in Batches** - Add all methods for a service at once before refactoring
7. **Document Patterns** - Note common patterns to speed up later services

---

## 🚀 Quick Start Commands

### Check Remaining Errors:
```bash
cd c:\Users\threk\Documents\Rechnungsfreigabe\backend
dotnet build 2>&1 | grep -c "error CS1061"
```

### Find Services with Most Errors:
```bash
dotnet build 2>&1 | grep "error CS1061" | grep -oP '(?<=Implementations/).*?(?=\.cs)' | sort | uniq -c | sort -rn
```

### Verify a Service is Fixed:
```bash
dotnet build 2>&1 | grep "[ServiceName].cs" | grep "error CS"
```

---

_Last Updated: Current Session_
_Current Status: UserService 90% complete (8/9 methods refactored), 14 services remaining_
