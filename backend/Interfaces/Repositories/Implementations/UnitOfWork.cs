using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Interfaces.Repositories;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IStatusRepository? _statuses;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IUserRoleRepository? _userRoles;
    private IPermissionRepository? _permissions;
    private IRepository<RolePermission>? _rolePermissions;
    private ICostCenterRepository? _costCenters;
    private IProjectRepository? _projects;
    private ISupplierRepository? _suppliers;
    private IPurchaseOrderRepository? _purchaseOrders;
    private IInvoiceRepository? _invoices;
    private IApprovalRuleRepository? _approvalRules;
    private IApprovalWorkflowRepository? _approvalWorkflows;
    private IInvoiceHistoryRepository? _invoiceHistories;
    private INotificationRepository? _notifications;
    private ISystemConfigRepository? _systemConfigs;
    private IEscalationRuleRepository? _escalationRules;
    private IEscalationLogRepository? _escalationLogs;
    private IRepository<EscalationRuleTriggerStatus>? _escalationRuleTriggerStatuses;
    private IRepository<EscalationRuleNotifyRole>? _escalationRuleNotifyRoles;
    private IRepository<EscalationRuleNotifyUser>? _escalationRuleNotifyUsers;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IStatusRepository Statuses => _statuses ??= new StatusRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IUserRoleRepository UserRoles => _userRoles ??= new UserRoleRepository(_context);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
    public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
    public ICostCenterRepository CostCenters => _costCenters ??= new CostCenterRepository(_context);
    public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
    public ISupplierRepository Suppliers => _suppliers ??= new SupplierRepository(_context);
    public IPurchaseOrderRepository PurchaseOrders => _purchaseOrders ??= new PurchaseOrderRepository(_context);
    public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
    public IApprovalRuleRepository ApprovalRules => _approvalRules ??= new ApprovalRuleRepository(_context);
    public IApprovalWorkflowRepository ApprovalWorkflows => _approvalWorkflows ??= new ApprovalWorkflowRepository(_context);
    public IInvoiceHistoryRepository InvoiceHistories => _invoiceHistories ??= new InvoiceHistoryRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public ISystemConfigRepository SystemConfigs => _systemConfigs ??= new SystemConfigRepository(_context);
    public IEscalationRuleRepository EscalationRules => _escalationRules ??= new EscalationRuleRepository(_context);
    public IEscalationLogRepository EscalationLogs => _escalationLogs ??= new EscalationLogRepository(_context);
    public IRepository<EscalationRuleTriggerStatus> EscalationRuleTriggerStatuses => _escalationRuleTriggerStatuses ??= new Repository<EscalationRuleTriggerStatus>(_context);
    public IRepository<EscalationRuleNotifyRole> EscalationRuleNotifyRoles => _escalationRuleNotifyRoles ??= new Repository<EscalationRuleNotifyRole>(_context);
    public IRepository<EscalationRuleNotifyUser> EscalationRuleNotifyUsers => _escalationRuleNotifyUsers ??= new Repository<EscalationRuleNotifyUser>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
