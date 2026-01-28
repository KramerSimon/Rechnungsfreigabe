using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Status> Statuses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<CostCenter> CostCenters { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<ApprovalRule> ApprovalRules { get; set; }
    public DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; }
    public DbSet<InvoiceHistory> InvoiceHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<EscalationRule> EscalationRules { get; set; }
    public DbSet<EscalationLog> EscalationLogs { get; set; }
    public DbSet<EscalationRuleTriggerStatus> EscalationRuleTriggerStatuses { get; set; }
    public DbSet<EscalationRuleNotifyRole> EscalationRuleNotifyRoles { get; set; }
    public DbSet<EscalationRuleNotifyUser> EscalationRuleNotifyUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to match MySQL database
        modelBuilder.Entity<Status>().ToTable("statuses");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserRole>().ToTable("user_roles");
        modelBuilder.Entity<Permission>().ToTable("permissions");
        modelBuilder.Entity<RolePermission>().ToTable("role_permissions");
        modelBuilder.Entity<CostCenter>().ToTable("cost_centers");
        modelBuilder.Entity<Project>().ToTable("projects");
        modelBuilder.Entity<Supplier>().ToTable("suppliers");
        modelBuilder.Entity<PurchaseOrder>().ToTable("purchase_orders");
        modelBuilder.Entity<Invoice>().ToTable("invoices");
        modelBuilder.Entity<ApprovalRule>().ToTable("approval_rules");
        modelBuilder.Entity<ApprovalWorkflow>().ToTable("approval_workflows");
        modelBuilder.Entity<InvoiceHistory>().ToTable("invoice_history");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        modelBuilder.Entity<SystemConfig>().ToTable("system_config");
        modelBuilder.Entity<EscalationRule>().ToTable("escalation_rules");
        modelBuilder.Entity<EscalationLog>().ToTable("escalation_logs");
        modelBuilder.Entity<EscalationRuleTriggerStatus>().ToTable("escalation_rule_trigger_statuses");
        modelBuilder.Entity<EscalationRuleNotifyRole>().ToTable("escalation_rule_notify_roles");
        modelBuilder.Entity<EscalationRuleNotifyUser>().ToTable("escalation_rule_notify_users");

        // Configure primary keys

        // Configure primary keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<CostCenter>()
            .HasKey(cc => cc.Id);

        modelBuilder.Entity<Project>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<PurchaseOrder>()
            .HasKey(po => po.Id);

        // Configure relationships
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CostCenter>()
            .HasOne(cc => cc.Manager)
            .WithMany(u => u.ManagedCostCenters)
            .HasForeignKey(cc => cc.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.CostCenter)
            .WithMany(cc => cc.Projects)
            .HasForeignKey(p => p.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.ProjectManager)
            .WithMany(u => u.ManagedProjects)
            .HasForeignKey(p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Status)
            .WithMany(s => s.Projects)
            .HasForeignKey(p => p.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.CostCenter)
            .WithMany(cc => cc.PurchaseOrders)
            .HasForeignKey(po => po.CostCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Project)
            .WithMany(p => p.PurchaseOrders)
            .HasForeignKey(po => po.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Creator)
            .WithMany()
            .HasForeignKey(po => po.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Approver)
            .WithMany()
            .HasForeignKey(po => po.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Status)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany(po => po.Invoices)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.CostCenter)
            .WithMany(cc => cc.Invoices)
            .HasForeignKey(i => i.CostCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Project)
            .WithMany(p => p.Invoices)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Creator)
            .WithMany(u => u.CreatedInvoices)
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Processor)
            .WithMany(u => u.ProcessedInvoices)
            .HasForeignKey(i => i.ProcessedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Status)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApprovalRule>()
            .HasOne(ar => ar.Creator)
            .WithMany()
            .HasForeignKey(ar => ar.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApprovalWorkflow>()
            .HasOne(aw => aw.Invoice)
            .WithMany(i => i.ApprovalWorkflows)
            .HasForeignKey(aw => aw.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApprovalWorkflow>()
            .HasOne(aw => aw.Rule)
            .WithMany(ar => ar.ApprovalWorkflows)
            .HasForeignKey(aw => aw.RuleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApprovalWorkflow>()
            .HasOne(aw => aw.Approver)
            .WithMany(u => u.ApprovalWorkflows)
            .HasForeignKey(aw => aw.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApprovalWorkflow>()
            .HasOne(aw => aw.Status)
            .WithMany(s => s.ApprovalWorkflows)
            .HasForeignKey(aw => aw.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InvoiceHistory>()
            .HasOne(ih => ih.Invoice)
            .WithMany(i => i.InvoiceHistories)
            .HasForeignKey(ih => ih.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceHistory>()
            .HasOne(ih => ih.ChangedByUser)
            .WithMany()
            .HasForeignKey(ih => ih.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure InvoiceHistory column mappings
        modelBuilder.Entity<InvoiceHistory>(entity =>
        {
            entity.ToTable("invoice_history");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.ActionType).HasColumnName("action_type").HasConversion<string>();
            entity.Property(e => e.ActionSource).HasColumnName("action_source").HasConversion<string>();
            entity.Property(e => e.OldStatus).HasColumnName("old_status");
            entity.Property(e => e.NewStatus).HasColumnName("new_status");
            entity.Property(e => e.FieldChanges).HasColumnName("field_changes");
            entity.Property(e => e.Comments).HasColumnName("comments");
            entity.Property(e => e.PolicyReference).HasColumnName("policy_reference");
            entity.Property(e => e.SystemReason).HasColumnName("system_reason");
            entity.Property(e => e.ImportChannel).HasColumnName("import_channel");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at");
        });

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Invoice)
            .WithMany(i => i.Notifications)
            .HasForeignKey(n => n.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // EscalationRule now uses comma-separated IDs instead of navigation properties
        // No foreign key relationships needed for NotifyUserIds and NotifyRoleIds

        modelBuilder.Entity<EscalationLog>()
            .HasOne(el => el.Invoice)
            .WithMany()
            .HasForeignKey(el => el.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationLog>()
            .HasOne(el => el.EscalationRule)
            .WithMany()
            .HasForeignKey(el => el.EscalationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationLog>()
            .HasOne(el => el.RecipientUser)
            .WithMany()
            .HasForeignKey(el => el.RecipientUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Escalation Rule junction tables
        modelBuilder.Entity<EscalationRuleTriggerStatus>()
            .HasKey(erts => new { erts.EscalationRuleId, erts.StatusId });

        modelBuilder.Entity<EscalationRuleTriggerStatus>()
            .HasOne(erts => erts.EscalationRule)
            .WithMany(er => er.TriggerStatuses)
            .HasForeignKey(erts => erts.EscalationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationRuleTriggerStatus>()
            .HasOne(erts => erts.Status)
            .WithMany()
            .HasForeignKey(erts => erts.StatusId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationRuleNotifyRole>()
            .HasKey(ernr => new { ernr.EscalationRuleId, ernr.RoleId });

        modelBuilder.Entity<EscalationRuleNotifyRole>()
            .HasOne(ernr => ernr.EscalationRule)
            .WithMany(er => er.NotifyRoles)
            .HasForeignKey(ernr => ernr.EscalationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationRuleNotifyRole>()
            .HasOne(ernr => ernr.Role)
            .WithMany()
            .HasForeignKey(ernr => ernr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationRuleNotifyUser>()
            .HasKey(ernu => new { ernu.EscalationRuleId, ernu.UserId });

        modelBuilder.Entity<EscalationRuleNotifyUser>()
            .HasOne(ernu => ernu.EscalationRule)
            .WithMany(er => er.NotifyUsers)
            .HasForeignKey(ernu => ernu.EscalationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EscalationRuleNotifyUser>()
            .HasOne(ernu => ernu.User)
            .WithMany()
            .HasForeignKey(ernu => ernu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SystemConfig>()
            .HasOne(sc => sc.UpdatedByUser)
            .WithMany()
            .HasForeignKey(sc => sc.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber);

        modelBuilder.Entity<SystemConfig>()
            .HasIndex(sc => sc.ConfigKey)
            .IsUnique();

        modelBuilder.Entity<EscalationRule>()
            .HasIndex(er => er.IsActive);

        // Configure enum conversions to strings (for non-Status enums)
        modelBuilder.Entity<ApprovalRule>()
            .Property(ar => ar.RuleType)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<SystemConfig>()
            .Property(sc => sc.DataType)
            .HasConversion<string>();
    }
}