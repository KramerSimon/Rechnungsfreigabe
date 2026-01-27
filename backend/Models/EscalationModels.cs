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

    [Required]
    [StringLength(50)]
    [Column("trigger_status")]
    public string TriggerStatus { get; set; } = "In_Pruefung";

    [Range(1, 240)]
    [Column("trigger_after_hours")]
    public int TriggerAfterHours { get; set; } = 48;

    [Range(1, 240)]
    [Column("repeat_interval_hours")]
    public int? RepeatIntervalHours { get; set; } = 24;

    [Range(0, 10)]
    [Column("max_escalations")]
    public int? MaxEscalations { get; set; } = 3;

    [StringLength(50)]
    [Column("notify_role")]
    public string? NotifyRole { get; set; }

    [Column("notify_role_id")]
    public int? NotifyRoleId { get; set; }

    [Column("notify_user_id")]
    public int? NotifyUserId { get; set; }

    [StringLength(500)]
    [Column("message_template")]
    public string? MessageTemplate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? NotifyUser { get; set; }
    public virtual Role? NotifyRoleRef { get; set; }
}
