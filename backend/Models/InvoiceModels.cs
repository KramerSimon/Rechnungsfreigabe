using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class Supplier
{
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    [Column("legal_name")]
    public string? LegalName { get; set; }

    [StringLength(30)]
    [Column("tax_number")]
    public string? TaxNumber { get; set; }

    [StringLength(30)]
    [Column("vat_number")]
    public string? VatNumber { get; set; }

    [StringLength(100)]
    [Column("address_line1")]
    public string? AddressLine1 { get; set; }

    [StringLength(100)]
    [Column("address_line2")]
    public string? AddressLine2 { get; set; }

    [StringLength(10)]
    [Column("postal_code")]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    [Column("city")]
    public string? City { get; set; }

    [StringLength(50)]
    [Column("country")]
    public string Country { get; set; } = "Deutschland";

    [EmailAddress]
    [StringLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [StringLength(30)]
    [Column("phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [Column("bank_name")]
    public string? BankName { get; set; }

    [StringLength(34)]
    [Column("iban")]
    public string? Iban { get; set; }

    [StringLength(11)]
    [Column("bic")]
    public string? Bic { get; set; }

    [Column("payment_terms_days")]
    public int PaymentTermsDays { get; set; } = 30;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class PurchaseOrder
{
    [Column("id")]
    [StringLength(20)]
    public string Id { get; set; } = string.Empty;

    [Column("title")]
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("cost_center_id")]
    [StringLength(20)]
    public string? CostCenterId { get; set; }

    [Column("project_id")]
    [StringLength(20)]
    public string? ProjectId { get; set; }

    [Column("total_amount", TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    [Column("status")]
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Offen;

    [Column("created_by")]
    public int CreatedBy { get; set; }
    
    [Column("approved_by")]
    public int? ApprovedBy { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("approved_at")]
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
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [StringLength(20)]
    [Column("purchase_order_id")]
    public string? PurchaseOrderId { get; set; }

    [StringLength(20)]
    [Column("cost_center_id")]
    public string? CostCenterId { get; set; }

    [StringLength(20)]
    [Column("project_id")]
    public string? ProjectId { get; set; }

    [Column("net_amount", TypeName = "decimal(12,2)")]
    public decimal NetAmount { get; set; }

    [Column("tax_amount", TypeName = "decimal(12,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column("total_amount", TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("invoice_date")]
    public DateTime InvoiceDate { get; set; }
    
    [Column("due_date")]
    public DateTime DueDate { get; set; }
    
    [Column("received_date")]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Eingegangen;

    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = true;
    
    [Column("approval_level")]
    public int ApprovalLevel { get; set; } = 1;
    
    [Column("auto_approved")]
    public bool AutoApproved { get; set; } = false;

    [StringLength(500)]
    [Column("pdf_file_path")]
    public string? PdfFilePath { get; set; }

    [Column("pdf_file_size")]
    public long? PdfFileSize { get; set; }

    [StringLength(255)]
    [Column("original_filename")]
    public string? OriginalFilename { get; set; }

    [Column("description")]
    public string? Description { get; set; }
    
    [Column("internal_notes")]
    public string? InternalNotes { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }
    
    [Column("processed_by")]
    public int? ProcessedBy { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
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