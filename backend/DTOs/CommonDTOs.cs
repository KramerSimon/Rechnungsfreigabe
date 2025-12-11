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
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class CreateRoleDto
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string[] Permissions { get; set; } = Array.Empty<string>();
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

// Project DTOs
public class ProjectDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CostCenterId { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal SpentAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public UserDto? ProjectManager { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProjectDto
{
    [Required]
    [StringLength(20)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string CostCenterId { get; set; } = string.Empty;

    public decimal Budget { get; set; } = 0;
    public string Status { get; set; } = "Geplant";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ProjectManagerId { get; set; }
}