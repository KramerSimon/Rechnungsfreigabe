using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/statuses")]
public class StatusesController : ControllerBase
{
    private readonly ApplicationDbContext context;

    public StatusesController(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Get all active statuses
    /// </summary>
    /// <returns>List of all active statuses</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Status>>> GetStatuses()
    {
        var statuses = await context.Statuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.EntityType)
            .ThenBy(s => s.SortOrder)
            .ToListAsync();

        return Ok(statuses);
    }

    /// <summary>
    /// Get statuses by entity type (Invoice, Project, PurchaseOrder, ApprovalWorkflow)
    /// </summary>
    /// <param name="entityType">The entity type to filter by</param>
    /// <returns>List of statuses for the given entity type</returns>
    [HttpGet("by-type/{entityType}")]
    public async Task<ActionResult<IEnumerable<Status>>> GetStatusesByType(string entityType)
    {
        var statuses = await context.Statuses
            .Where(s => s.EntityType == entityType && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        if (!statuses.Any())
            return NotFound(new { message = $"No statuses found for entity type: {entityType}" });

        return Ok(statuses);
    }

    /// <summary>
    /// Get a specific status by code and entity type
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="code">The status code</param>
    /// <returns>The matching status or 404 if not found</returns>
    [HttpGet("{entityType}/{code}")]
    public async Task<ActionResult<Status>> GetStatusByCode(string entityType, string code)
    {
        var status = await context.Statuses
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.Code == code && s.IsActive);

        if (status == null)
            return NotFound(new { message = $"Status not found: {code} for entity type: {entityType}" });

        return Ok(status);
    }
}
