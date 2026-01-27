using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RechnungsfreigabeAPI.Models;

/// <summary>
/// Standard status model that is used across the system for invoices, purchase orders, projects, and approval workflows.
/// This centralized status table ensures consistency and allows for dynamic status management.
/// </summary>
[Table("statuses")]
public class Status
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the status (e.g., "Eingegangen", "Freigegeben", "Approved", "Pending")
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the status in the UI
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this status means
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this status applies to: Invoice, PurchaseOrder, Project, ApprovalWorkflow
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for displaying statuses in the UI
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this status is active and available for use
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Hex color code for UI display (e.g., "#4CAF50" for green)
    /// </summary>
    [StringLength(7)]
    [Column("color")]
    public string Color { get; set; } = "#808080";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    
    [JsonIgnore]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    
    [JsonIgnore]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    
    [JsonIgnore]
    public virtual ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
}

/// <summary>
/// Entity type constants for the Status table
/// </summary>
public static class EntityTypes
{
    public const string Invoice = "Invoice";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string Project = "Project";
    public const string ApprovalWorkflow = "ApprovalWorkflow";
}

/// <summary>
/// Standard status codes - preserved as constants for backward compatibility
/// These match the codes in the statuses table
/// </summary>
public static class StatusCodes
{
    // Invoice Status Codes
    public static class Invoice
    {
        public const string Eingegangen = "Received";
        public const string InPruefung = "Under_Review";
        public const string FreigabeErforderlich = "Approval_Required";
        public const string Freigegeben = "Approved";
        public const string Abgelehnt = "Rejected";
        public const string Bezahlt = "Paid";
        public const string Ueberfaellig = "Overdue";
        public const string Storniert = "Cancelled";
    }

    // Purchase Order Status Codes
    public static class PurchaseOrder
    {
        public const string Offen = "Open";
        public const string TeilweiseErfuellt = "Partially_Fulfilled";
        public const string Erfuellt = "Fulfilled";
        public const string Storniert = "Cancelled";
    }

    // Project Status Codes
    public static class Project
    {
        public const string Geplant = "Planned";
        public const string Aktiv = "Active";
        public const string Pausiert = "On_Hold";
        public const string Abgeschlossen = "Completed";
        public const string Abgebrochen = "Cancelled";
    }

    // Approval Workflow Status Codes
    public static class ApprovalWorkflow
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Skipped = "Skipped";
        public const string Waiting = "Waiting";
    }
}
