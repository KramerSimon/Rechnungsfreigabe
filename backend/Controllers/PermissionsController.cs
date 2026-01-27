using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PermissionDto>>> GetAllPermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionDto>> GetPermissionById(int id)
    {
        var permission = await _permissionService.GetPermissionByIdAsync(id);
        if (permission == null)
            return NotFound(new { message = $"Permission with ID {id} not found" });

        return Ok(permission);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PermissionDto>> CreatePermission(CreatePermissionDto dto)
    {
        var permission = await _permissionService.CreatePermissionAsync(dto);
        return CreatedAtAction(nameof(GetPermissionById), new { id = permission.Id }, permission);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PermissionDto>> UpdatePermission(int id, UpdatePermissionDto dto)
    {
        try
        {
            var permission = await _permissionService.UpdatePermissionAsync(id, dto);
            return Ok(permission);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        try
        {
            await _permissionService.DeletePermissionAsync(id);
            return Ok(new { message = "Permission deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
