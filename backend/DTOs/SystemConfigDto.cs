using System.ComponentModel.DataAnnotations;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.DTOs;

public class SystemConfigDto
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public ConfigDataType DataType { get; set; }
    public string? Description { get; set; }
    public bool IsEditable { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertSystemConfigDto
{
    [Required]
    [StringLength(100)]
    public string ConfigKey { get; set; } = string.Empty;

    public string? ConfigValue { get; set; }

    public ConfigDataType? DataType { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? IsEditable { get; set; }
}
