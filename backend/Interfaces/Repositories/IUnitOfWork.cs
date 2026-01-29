using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IStatusRepository Statuses { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IUserRoleRepository UserRoles { get; }
    IPermissionRepository Permissions { get; }
    IRepository<RolePermission> RolePermissions { get; }
    ICostCenterRepository CostCenters { get; }
    IProjectRepository Projects { get; }
    ISupplierRepository Suppliers { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    IInvoiceRepository Invoices { get; }
    IApprovalRuleRepository ApprovalRules { get; }
    IApprovalWorkflowRepository ApprovalWorkflows { get; }
    IInvoiceHistoryRepository InvoiceHistories { get; }
    INotificationRepository Notifications { get; }
    ISystemConfigRepository SystemConfigs { get; }
    IEscalationRuleRepository EscalationRules { get; }
    IEscalationLogRepository EscalationLogs { get; }
    IRepository<EscalationRuleTriggerStatus> EscalationRuleTriggerStatuses { get; }
    IRepository<EscalationRuleNotifyRole> EscalationRuleNotifyRoles { get; }
    IRepository<EscalationRuleNotifyUser> EscalationRuleNotifyUsers { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

