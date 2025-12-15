using System.ComponentModel.DataAnnotations;

namespace RechnungsfreigabeAPI.DTOs;

public class PurchaseOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = string.Empty;
    public UserDto? Creator { get; set; }
    public UserDto? Approver { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CreatePurchaseOrderDto
{
    [Required]
    [StringLength(20)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(20)]
    public string? CostCenterId { get; set; }

    [StringLength(20)]
    public string? ProjectId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";
}
