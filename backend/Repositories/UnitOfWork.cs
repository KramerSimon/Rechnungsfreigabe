using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Repositories.Interfaces;

namespace RechnungsfreigabeAPI.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<Status>? _statuses;
    private IRepository<User>? _users;
    private IRepository<Role>? _roles;
    private IRepository<UserRole>? _userRoles;
    private IRepository<Permission>? _permissions;
    private IRepository<RolePermission>? _rolePermissions;
    private IRepository<CostCenter>? _costCenters;
    private IRepository<Project>? _projects;
    private IRepository<Supplier>? _suppliers;
    private IRepository<PurchaseOrder>? _purchaseOrders;
    private IRepository<Invoice>? _invoices;
    private IRepository<ApprovalRule>? _approvalRules;
    private IRepository<ApprovalWorkflow>? _approvalWorkflows;
    private IRepository<InvoiceHistory>? _invoiceHistories;
    private IRepository<Notification>? _notifications;
    private IRepository<SystemConfig>? _systemConfigs;
    private IRepository<EscalationRule>? _escalationRules;
    private IRepository<EscalationLog>? _escalationLogs;
    private IRepository<EscalationRuleTriggerStatus>? _escalationRuleTriggerStatuses;
    private IRepository<EscalationRuleNotifyRole>? _escalationRuleNotifyRoles;
    private IRepository<EscalationRuleNotifyUser>? _escalationRuleNotifyUsers;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<Status> Statuses => _statuses ??= new Repository<Status>(_context);
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
    public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
    public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
    public IRepository<CostCenter> CostCenters => _costCenters ??= new Repository<CostCenter>(_context);
    public IRepository<Project> Projects => _projects ??= new Repository<Project>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IRepository<PurchaseOrder> PurchaseOrders => _purchaseOrders ??= new Repository<PurchaseOrder>(_context);
    public IRepository<Invoice> Invoices => _invoices ??= new Repository<Invoice>(_context);
    public IRepository<ApprovalRule> ApprovalRules => _approvalRules ??= new Repository<ApprovalRule>(_context);
    public IRepository<ApprovalWorkflow> ApprovalWorkflows => _approvalWorkflows ??= new Repository<ApprovalWorkflow>(_context);
    public IRepository<InvoiceHistory> InvoiceHistories => _invoiceHistories ??= new Repository<InvoiceHistory>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
    public IRepository<SystemConfig> SystemConfigs => _systemConfigs ??= new Repository<SystemConfig>(_context);
    public IRepository<EscalationRule> EscalationRules => _escalationRules ??= new Repository<EscalationRule>(_context);
    public IRepository<EscalationLog> EscalationLogs => _escalationLogs ??= new Repository<EscalationLog>(_context);
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
