namespace RechnungsfreigabeAPI.DTOs;

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsSystemPermission { get; set; }
}

public class CreatePermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsSystemPermission { get; set; } = false;
}

public class UpdatePermissionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}
