using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class EscalationRule
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Range(1, 14400)]
    [Column("trigger_after_minutes")]
    public int TriggerAfterMinutes { get; set; } = 2880; // Default: 48 hours = 2880 minutes

    [Range(1, 240)]
    [Column("repeat_interval_hours")]
    public int? RepeatIntervalHours { get; set; } = 24;

    [Range(0, 10)]
    [Column("max_escalations")]
    public int? MaxEscalations { get; set; } = 3;

    [StringLength(2000)]
    [Column("message_template")]
    public string? MessageTemplate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<EscalationRuleTriggerStatus> TriggerStatuses { get; set; } = new List<EscalationRuleTriggerStatus>();
    public virtual ICollection<EscalationRuleNotifyRole> NotifyRoles { get; set; } = new List<EscalationRuleNotifyRole>();
    public virtual ICollection<EscalationRuleNotifyUser> NotifyUsers { get; set; } = new List<EscalationRuleNotifyUser>();
}

public class EscalationRuleTriggerStatus
{
    [Column("escalation_rule_id")]
    public int EscalationRuleId { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual EscalationRule EscalationRule { get; set; } = null!;
    public virtual Status Status { get; set; } = null!;
}

public class EscalationRuleNotifyRole
{
    [Column("escalation_rule_id")]
    public int EscalationRuleId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual EscalationRule EscalationRule { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

public class EscalationRuleNotifyUser
{
    [Column("escalation_rule_id")]
    public int EscalationRuleId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual EscalationRule EscalationRule { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
