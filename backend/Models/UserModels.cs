using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(255)]
    [Column("active_directory_sid")]
    public string? ActiveDirectorySid { get; set; }

    [Column("password_changed_at")]
    public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;
    
    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; } = 0;
    
    [Column("locked_until")]
    public DateTime? LockedUntil { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<CostCenter> ManagedCostCenters { get; set; } = new List<CostCenter>();
    public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public virtual ICollection<Invoice> CreatedInvoices { get; set; } = new List<Invoice>();
    public virtual ICollection<Invoice> ProcessedInvoices { get; set; } = new List<Invoice>();
    public virtual ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Role
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("permissions", TypeName = "json")]
    public string Permissions { get; set; } = "[]";

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    [Key, Column("user_id", Order = 0)]
    public int UserId { get; set; }

    [Key, Column("role_id", Order = 1)]
    public int RoleId { get; set; }

    [Column("assigned_at", TypeName = "timestamp")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

public class CostCenter
{
    [StringLength(20)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("manager_id")]
    public int? ManagerId { get; set; }

    [Column("budget", TypeName = "decimal(12,2)")]
    public decimal Budget { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? Manager { get; set; }
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public class Project
{
    [StringLength(20)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    [Column("cost_center_id")]
    public string CostCenterId { get; set; } = string.Empty;

    [Column("budget", TypeName = "decimal(12,2)")]
    public decimal Budget { get; set; } = 0;

    [Column("spent_amount", TypeName = "decimal(12,2)")]
    public decimal SpentAmount { get; set; } = 0;

    [Column("status")]
    public ProjectStatus Status { get; set; } = ProjectStatus.Geplant;

    [Column("start_date")]
    public DateTime? StartDate { get; set; }
    
    [Column("end_date")]
    public DateTime? EndDate { get; set; }
    
    [Column("project_manager_id")]
    public int? ProjectManagerId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CostCenter CostCenter { get; set; } = null!;
    public virtual User? ProjectManager { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public enum ProjectStatus
{
    Geplant,
    Aktiv,
    Pausiert,
    Abgeschlossen,
    Abgebrochen
}