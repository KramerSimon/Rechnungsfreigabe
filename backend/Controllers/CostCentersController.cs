using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;

using RechnungsfreigabeAPI.Services.Interfaces;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/cost-centers")]
// [Authorize] // Temporarily disabled for testing
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService costCenterService;
    public CostCentersController(ICostCenterService costCenterService)
    {
        this.costCenterService = costCenterService;
        }

    /// <summary>
    /// Get all cost centers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CostCenterDto>>> GetCostCenters()
    {
        try
        {
            var costCenters = await costCenterService.GetAllCostCentersAsync();
            return Ok(costCenters);
        }
        catch (Exception)
        {
            
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
            var costCenter = await costCenterService.GetCostCenterByIdAsync(id);
            
            if (costCenter == null)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return Ok(costCenter);
        }
        catch (Exception)
        {
            
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
            var projects = await costCenterService.GetCostCenterProjectsAsync(id);
            return Ok(projects);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving projects" });
        }
    }

    /// <summary>
    /// Get all projects from all cost centers
    /// </summary>
    [HttpGet("all/projects")]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
    {
        try
        {
            var allProjects = await costCenterService.GetAllProjectsAsync();
            return Ok(allProjects);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving all projects" });
        }
    }

    /// <summary>
    /// Create a new project for a cost center
    /// </summary>
    [HttpPost("{costCenterId}/projects")]
    public async Task<ActionResult<ProjectDto>> CreateProject(string costCenterId, [FromBody] CreateProjectDto createProjectDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Ensure the cost center ID matches
            createProjectDto.CostCenterId = costCenterId;

            var project = await costCenterService.CreateProjectAsync(createProjectDto);
            
            return Created($"/api/costcenters/{costCenterId}/projects/{project.Id}", project);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the project" });
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

            var costCenter = await costCenterService.CreateCostCenterAsync(createCostCenterDto);
            
            return CreatedAtAction(nameof(GetCostCenter), new { id = costCenter.Id }, costCenter);
        }
        catch (Exception)
        {
            
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

            var costCenter = await costCenterService.UpdateCostCenterAsync(id, updateCostCenterDto);
            
            if (costCenter == null)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return Ok(costCenter);
        }
        catch (Exception)
        {
            
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
            var success = await costCenterService.DeleteCostCenterAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Cost center with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the cost center" });
        }
    }
}