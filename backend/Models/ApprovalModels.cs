using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class ApprovalRule
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public RuleType RuleType { get; set; } = RuleType.Manual;

    public int Priority { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    [Required]
    [Column(TypeName = "json")]
    public string Conditions { get; set; } = "[]";

    [Required]
    [Column(TypeName = "json")]
    public string Actions { get; set; } = "[]";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
}

public class ApprovalWorkflow
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public int? RuleId { get; set; }
    public int StepNumber { get; set; }
    public int ApproverId { get; set; }
    public int ApprovalLevel { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public string? Comments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual ApprovalRule? Rule { get; set; }
    public virtual User Approver { get; set; } = null!;
}

public class InvoiceHistory
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [StringLength(20)]
    public string? OldStatus { get; set; }

    [StringLength(20)]
    public string? NewStatus { get; set; }

    [Column(TypeName = "json")]
    public string? FieldChanges { get; set; }

    public string? Comments { get; set; }

    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual User ChangedByUser { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? InvoiceId { get; set; }

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [StringLength(500)]
    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
}

public class SystemConfig
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    public string? ConfigValue { get; set; }

    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    public string? Description { get; set; }

    public bool IsEditable { get; set; } = true;

    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? UpdatedByUser { get; set; }
}

public enum RuleType
{
    Automatic,
    Manual
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Skipped
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum ConfigDataType
{
    String,
    Number,
    Boolean,
    Json
}