using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/system-config")]
[Authorize(Roles = "Administrator")]
public class SystemConfigController : ControllerBase
{
    private readonly ISystemConfigService systemConfigService;

    public SystemConfigController(ISystemConfigService systemConfigService)
    {
        this.systemConfigService = systemConfigService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SystemConfigDto>>> GetAll()
    {
        try
        {
            var configs = await systemConfigService.GetAllAsync();
            return Ok(configs);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Fehler beim Laden der Systemeinstellungen" });
        }
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<SystemConfigDto>> GetByKey(string key)
    {
        try
        {
            var config = await systemConfigService.GetByKeyAsync(key);
            if (config == null)
            {
                return NotFound(new { message = "Eintrag nicht gefunden" });
            }

            return Ok(config);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Fehler beim Laden der Systemeinstellung" });
        }
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<SystemConfigDto>> Upsert(string key, [FromBody] UpsertSystemConfigDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Favor route key over body value to avoid mismatches.
            dto.ConfigKey = key;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;

            var updated = await systemConfigService.UpsertAsync(key, dto, userId);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Fehler beim Speichern der Systemeinstellung" });
        }
    }
}
