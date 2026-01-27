using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechnungsfreigabeAPI.Models;

public class Permission
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
    [Column("code")]
    public string Code { get; set; } = string.Empty; // e.g., "invoices.read", "users.edit"

    [Column("category")]
    public string? Category { get; set; } // e.g., "invoices", "users", "reports"

    [Column("is_system_permission")]
    public bool IsSystemPermission { get; set; } = false; // Built-in permission

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    [Key, Column("role_id", Order = 0)]
    public int RoleId { get; set; }

    [Key, Column("permission_id", Order = 1)]
    public int PermissionId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
