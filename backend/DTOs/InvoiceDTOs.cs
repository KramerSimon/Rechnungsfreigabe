using System.ComponentModel.DataAnnotations;

namespace RechnungsfreigabeAPI.DTOs;

// Supplier DTOs
public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "Deutschland";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public int PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSupplierDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string? LegalName { get; set; }

    [StringLength(30)]
    public string? TaxNumber { get; set; }

    [StringLength(30)]
    public string? VatNumber { get; set; }

    [StringLength(100)]
    public string? AddressLine1 { get; set; }

    [StringLength(100)]
    public string? AddressLine2 { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string Country { get; set; } = "Deutschland";

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(34)]
    public string? Iban { get; set; }

    [StringLength(11)]
    public string? Bic { get; set; }

    public int PaymentTermsDays { get; set; } = 30;
}

// Invoice DTOs
public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public SupplierDto Supplier { get; set; } = null!;
    public string? PurchaseOrderId { get; set; }
    public string? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public int ApprovalLevel { get; set; }
    public bool AutoApproved { get; set; }
    public string? PdfFilePath { get; set; }
    public long? PdfFileSize { get; set; }
    public string? OriginalFilename { get; set; }
    public string? Description { get; set; }
    public string? InternalNotes { get; set; }
    public UserDto? Creator { get; set; }
    public UserDto? Processor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
    public ApprovalWorkflowDto[] PendingApprovals { get; set; } = Array.Empty<ApprovalWorkflowDto>();
}

public class CreateInvoiceDto
{
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public int SupplierId { get; set; }

    [StringLength(20)]
    public string? PurchaseOrderId { get; set; }

    [StringLength(20)]
    public string? CostCenterId { get; set; }

    [StringLength(20)]
    public string? ProjectId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal NetAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; } = 0;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public string? Description { get; set; }
    public string? InternalNotes { get; set; }

    public bool RequiresApproval { get; set; } = true;
}

public class UpdateInvoiceDto
{
    [StringLength(50)]
    public string? InvoiceNumber { get; set; }

    public int? SupplierId { get; set; }

    [StringLength(20)]
    public string? CostCenterId { get; set; }

    [StringLength(20)]
    public string? ProjectId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? NetAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TaxAmount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? TotalAmount { get; set; }

    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
    public string? InternalNotes { get; set; }
    public string? Status { get; set; }
}

// Approval DTOs
public class ApprovalWorkflowDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? RuleId { get; set; }
    public string? RuleName { get; set; }
    public int StepNumber { get; set; }
    public UserDto Approver { get; set; } = null!;
    public int ApprovalLevel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApproveInvoiceDto
{
    [Required]
    public bool Approved { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }
}

public class DashboardStatsDto
{
    public int NewInvoices { get; set; }
    public int PendingApproval { get; set; }
    public int ApprovedInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal MonthlyApprovedAmount { get; set; }
    public decimal PendingApprovalAmount { get; set; }
}

// Notification DTOs
public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public InvoiceDto? Invoice { get; set; }
}

// Paging DTOs
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class PageRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
}

// Invoice History DTOs
public class InvoiceHistoryDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionSource { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public Dictionary<string, object>? FieldChanges { get; set; }
    public string? Comments { get; set; }
    public string? PolicyReference { get; set; }
    public string? SystemReason { get; set; }
    public UserDto? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ImportChannel { get; set; }
    public string DisplayIcon { get; set; } = string.Empty; // For UI rendering
    public string DisplayColor { get; set; } = string.Empty; // For UI rendering
}

public class FieldChangeDto
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class CreateHistoryEntryDto
{
    public int InvoiceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionSource { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public List<FieldChangeDto>? FieldChanges { get; set; }
    public string? Comments { get; set; }
    public string? PolicyReference { get; set; }
    public string? SystemReason { get; set; }
    public int? ChangedBy { get; set; }
    public string? ImportChannel { get; set; }
}

public class InvoiceHistoryTimelineDto
{
    public string Date { get; set; } = string.Empty; // "HEUTE", "GESTERN", "10. DEZEMBER 2023"
    public List<InvoiceHistoryDto> Entries { get; set; } = new List<InvoiceHistoryDto>();
}