using System.ComponentModel.DataAnnotations;

namespace RechnungsfreigabeAPI.DTOs;

public class EscalationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> TriggerStatusIds { get; set; } = new();
    public List<StatusDto> TriggerStatuses { get; set; } = new();
    public int TriggerAfterMinutes { get; set; }
    public int? RepeatIntervalHours { get; set; }
    public int? MaxEscalations { get; set; }
    public List<int> NotifyRoleIds { get; set; } = new();
    public List<RoleDto> NotifyRoles { get; set; } = new();
    public List<int> NotifyUserIds { get; set; } = new();
    public List<UserDto> NotifyUsers { get; set; } = new();
    public string? MessageTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StatusDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class CreateEscalationRuleDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public List<int> TriggerStatusIds { get; set; } = new();

    [Range(1, 14400)]
    public int TriggerAfterMinutes { get; set; } = 2880; // Default: 48 hours = 2880 minutes

    [Range(1, 240)]
    public int? RepeatIntervalHours { get; set; } = 24;

    [Range(0, 10)]
    public int? MaxEscalations { get; set; } = 3;

    public List<int> NotifyRoleIds { get; set; } = new();

    public List<int> NotifyUserIds { get; set; } = new();

    [StringLength(2000)]
    public string? MessageTemplate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateEscalationRuleDto
{
    [StringLength(100)]
    public string? Name { get; set; }
    
    public string? Description { get; set; }

    public List<int>? TriggerStatusIds { get; set; }

    [Range(1, 14400)]
    public int? TriggerAfterMinutes { get; set; }

    [Range(1, 240)]
    public int? RepeatIntervalHours { get; set; }

    [Range(0, 10)]
    public int? MaxEscalations { get; set; }

    public List<int>? NotifyRoleIds { get; set; }

    public List<int>? NotifyUserIds { get; set; }

    [StringLength(2000)]
    public string? MessageTemplate { get; set; }

    public bool? IsActive { get; set; }
}
