using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class Supplier
{
    public int Id { get; set; }

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
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class PurchaseOrder
{
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

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Offen;

    public int CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual CostCenter? CostCenter { get; set; }
    public virtual Project? Project { get; set; }
    public virtual User Creator { get; set; } = null!;
    public virtual User? Approver { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class Invoice
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    [StringLength(20)]
    public string? PurchaseOrderId { get; set; }

    [StringLength(20)]
    public string? CostCenterId { get; set; }

    [StringLength(20)]
    public string? ProjectId { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal NetAmount { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Eingegangen;

    public bool RequiresApproval { get; set; } = true;
    public int ApprovalLevel { get; set; } = 1;
    public bool AutoApproved { get; set; } = false;

    [StringLength(500)]
    public string? PdfFilePath { get; set; }

    public long? PdfFileSize { get; set; }

    [StringLength(255)]
    public string? OriginalFilename { get; set; }

    public string? Description { get; set; }
    public string? InternalNotes { get; set; }

    public int? CreatedBy { get; set; }
    public int? ProcessedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual CostCenter? CostCenter { get; set; }
    public virtual Project? Project { get; set; }
    public virtual User? Creator { get; set; }
    public virtual User? Processor { get; set; }
    public virtual ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
    public virtual ICollection<InvoiceHistory> InvoiceHistories { get; set; } = new List<InvoiceHistory>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public enum PurchaseOrderStatus
{
    Offen,
    Teilweise_Erfuellt,
    Erfuellt,
    Storniert
}

public enum InvoiceStatus
{
    Eingegangen,
    In_Pruefung,
    Freigabe_Erforderlich,
    Freigegeben,
    Abgelehnt,
    Bezahlt,
    Ueberfaellig,
    Storniert
}