using System.ComponentModel.DataAnnotations;

namespace RechnungsfreigabeAPI.DTOs;

// Auth DTOs
public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

// User DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public RoleDto[] Roles { get; set; } = Array.Empty<RoleDto>();
}

public class CreateUserDto
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ActiveDirectorySid { get; set; }

    public int[] RoleIds { get; set; } = Array.Empty<int>();
}

public class UpdateUserDto
{
    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public bool? IsActive { get; set; }

    public int[]? RoleIds { get; set; }
}

// Role DTOs
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<object> Permissions { get; set; } = new();
    public string? Color { get; set; } = "#ff9800"; // Default orange color
    public bool IsSystemRole { get; set; } = false;
}

public class CreateRoleDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<object>? Permissions { get; set; }

    [StringLength(7)]
    public string? Color { get; set; } = "#ff9800";

    public bool? IsSystemRole { get; set; }
}

public class UpdateRoleDto
{
    [StringLength(50)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public List<object>? Permissions { get; set; }

    [StringLength(7)]
    public string? Color { get; set; }

    public bool? IsSystemRole { get; set; }
}

// Cost Center DTOs
public class CostCenterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Budget { get; set; }
    public bool IsActive { get; set; }
    public UserDto? Manager { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCostCenterDto
{
    [Required]
    [StringLength(20)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal Budget { get; set; } = 0;
    public int? ManagerId { get; set; }
}