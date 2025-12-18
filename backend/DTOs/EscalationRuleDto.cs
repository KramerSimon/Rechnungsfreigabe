using System.ComponentModel.DataAnnotations;

namespace RechnungsfreigabeAPI.DTOs;

public class EscalationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerStatus { get; set; } = string.Empty;
    public int TriggerAfterHours { get; set; }
    public int? RepeatIntervalHours { get; set; }
    public int? MaxEscalations { get; set; }
    public string? NotifyRole { get; set; }
    public int? NotifyUserId { get; set; }
    public string? NotifyUserName { get; set; }
    public string? MessageTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEscalationRuleDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string TriggerStatus { get; set; } = string.Empty;

    [Range(1, 240)]
    public int TriggerAfterHours { get; set; } = 48;

    [Range(1, 240)]
    public int? RepeatIntervalHours { get; set; } = 24;

    [Range(0, 10)]
    public int? MaxEscalations { get; set; } = 3;

    [StringLength(50)]
    public string? NotifyRole { get; set; }

    public int? NotifyUserId { get; set; }

    [StringLength(500)]
    public string? MessageTemplate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateEscalationRuleDto
{
    [StringLength(100)]
    public string? Name { get; set; }
    public string? Description { get; set; }

    [StringLength(50)]
    public string? TriggerStatus { get; set; }

    [Range(1, 240)]
    public int? TriggerAfterHours { get; set; }

    [Range(1, 240)]
    public int? RepeatIntervalHours { get; set; }

    [Range(0, 10)]
    public int? MaxEscalations { get; set; }

    [StringLength(50)]
    public string? NotifyRole { get; set; }

    public int? NotifyUserId { get; set; }

    [StringLength(500)]
    public string? MessageTemplate { get; set; }

    public bool? IsActive { get; set; }
}
