using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RechnungsfreigabeAPI.Models;

public class ApprovalRule
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("rule_type")]
    public RuleType RuleType { get; set; } = RuleType.Manual;

    [Column("priority")]
    public int Priority { get; set; } = 10;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("conditions", TypeName = "json")]
    public string Conditions { get; set; } = "[]";

    [Required]
    [Column("actions", TypeName = "json")]
    public string Actions { get; set; } = "[]";

    [Column("created_by")]
    public int CreatedBy { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public virtual User Creator { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
}

public class ApprovalWorkflow
{
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    
    [Column("rule_id")]
    public int? RuleId { get; set; }
    
    [Column("step_number")]
    public int StepNumber { get; set; }
    
    [Column("approver_id")]
    public int ApproverId { get; set; }
    
    [Column("approval_level")]
    public int ApprovalLevel { get; set; }

    [Column("status_id")]
    public int? StatusId { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }
    
    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual ApprovalRule? Rule { get; set; }
    public virtual User Approver { get; set; } = null!;
    public virtual Status? Status { get; set; }
}

public enum HistoryActionType
{
    Created,
    Updated, 
    StatusChanged,
    Approved,
    Rejected,
    Escalated,
    Assigned,
    SystemAction,
    DataCompleted,
    PaymentInitiated,
    PolicyTriggered
}

public enum HistoryActionSource
{
    User,
    System,
    Policy,
    Escalation,
    Import
}

public class InvoiceHistory
{
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("action_type")]
    public HistoryActionType ActionType { get; set; }

    [Column("action_source")]
    public HistoryActionSource ActionSource { get; set; }

    [StringLength(20)]
    [Column("old_status")]
    public string? OldStatus { get; set; }

    [StringLength(20)]
    [Column("new_status")]
    public string? NewStatus { get; set; }

    [Column("field_changes", TypeName = "json")]
    public string? FieldChanges { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [StringLength(100)]
    [Column("policy_reference")]
    public string? PolicyReference { get; set; }

    [StringLength(100)]
    [Column("system_reason")]
    public string? SystemReason { get; set; }

    [Column("changed_by")]
    public int? ChangedBy { get; set; } // Nullable for system actions
    
    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    [Column("import_channel")]
    public string? ImportChannel { get; set; } // E-Mail, API, Manual, etc.

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual User? ChangedByUser { get; set; }
}

public class Notification
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("invoice_id")]
    public int? InvoiceId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("priority")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [StringLength(500)]
    [Column("action_url")]
    public string? ActionUrl { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
}

public class SystemConfig
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("config_key")]
    public string ConfigKey { get; set; } = string.Empty;

    [Column("config_value")]
    public string? ConfigValue { get; set; }

    [Column("data_type")]
    public ConfigDataType DataType { get; set; } = ConfigDataType.String;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_editable")]
    public bool IsEditable { get; set; } = true;

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? UpdatedByUser { get; set; }
}

public enum RuleType
{
    Automatic,
    Manual
}

// ApprovalStatus enum removed - statuses are now in the centralized statuses table
// Use StatusCodes.ApprovalWorkflow.* constants instead

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