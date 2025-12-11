using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService _costCenterService;
    private readonly ILogger<CostCentersController> _logger;

    public CostCentersController(ICostCenterService costCenterService, ILogger<CostCentersController> logger)
    {
        _costCenterService = costCenterService;
        _logger = logger;
    }

    /// <summary>
    /// Get all cost centers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CostCenterDto>>> GetCostCenters()
    {
        try
        {
            var costCenters = await _costCenterService.GetAllCostCentersAsync();
            return Ok(costCenters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost centers");
            return StatusCode(500, new { message = "An error occurred while retrieving cost centers" });
        }
    }

    /// <summary>
    /// Get cost center by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CostCenterDto>> GetCostCenter(string id)
    {
        try
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            
            if (costCenter == null)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return Ok(costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost center with ID: {CostCenterId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the cost center" });
        }
    }

    /// <summary>
    /// Get projects for a cost center
    /// </summary>
    [HttpGet("{id}/projects")]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetCostCenterProjects(string id)
    {
        try
        {
            var projects = await _costCenterService.GetCostCenterProjectsAsync(id);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for cost center: {CostCenterId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving projects" });
        }
    }

    /// <summary>
    /// Create a new cost center
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CostCenterDto>> CreateCostCenter([FromBody] CreateCostCenterDto createCostCenterDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var costCenter = await _costCenterService.CreateCostCenterAsync(createCostCenterDto);
            
            return CreatedAtAction(nameof(GetCostCenter), new { id = costCenter.Id }, costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cost center: {CostCenterId}", createCostCenterDto.Id);
            return StatusCode(500, new { message = "An error occurred while creating the cost center" });
        }
    }

    /// <summary>
    /// Update an existing cost center
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CostCenterDto>> UpdateCostCenter(string id, [FromBody] CreateCostCenterDto updateCostCenterDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var costCenter = await _costCenterService.UpdateCostCenterAsync(id, updateCostCenterDto);
            
            if (costCenter == null)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return Ok(costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cost center with ID: {CostCenterId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the cost center" });
        }
    }

    /// <summary>
    /// Delete a cost center
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCostCenter(string id)
    {
        try
        {
            var success = await _costCenterService.DeleteCostCenterAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cost center with ID: {CostCenterId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the cost center" });
        }
    }
}