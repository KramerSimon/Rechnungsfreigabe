using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

/// <summary>
/// Model to track escalation emails sent for audit and monitoring purposes
/// </summary>
[Table("escalation_logs")]
public class EscalationLog
{
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    [Required]
    public int InvoiceId { get; set; }

    [Column("escalation_rule_id")]
    [Required]
    public int EscalationRuleId { get; set; }

    [Column("recipient_user_id")]
    public int? RecipientUserId { get; set; }

    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Sent"; // Sent, Failed, etc.

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
    public virtual EscalationRule? EscalationRule { get; set; }
    public virtual User? RecipientUser { get; set; }
}
