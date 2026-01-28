using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Status> Statuses { get; }
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<RolePermission> RolePermissions { get; }
    IRepository<CostCenter> CostCenters { get; }
    IRepository<Project> Projects { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<PurchaseOrder> PurchaseOrders { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<ApprovalRule> ApprovalRules { get; }
    IRepository<ApprovalWorkflow> ApprovalWorkflows { get; }
    IRepository<InvoiceHistory> InvoiceHistories { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<SystemConfig> SystemConfigs { get; }
    IRepository<EscalationRule> EscalationRules { get; }
    IRepository<EscalationLog> EscalationLogs { get; }
    IRepository<EscalationRuleTriggerStatus> EscalationRuleTriggerStatuses { get; }
    IRepository<EscalationRuleNotifyRole> EscalationRuleNotifyRoles { get; }
    IRepository<EscalationRuleNotifyUser> EscalationRuleNotifyUsers { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
