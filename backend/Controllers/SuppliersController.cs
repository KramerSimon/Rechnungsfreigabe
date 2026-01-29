using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Interfaces.Services.Implementations;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService supplierService;
    public SuppliersController(ISupplierService supplierService)
    {
        this.supplierService = supplierService;
        }

    /// <summary>
    /// Get all suppliers
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
    {
        try
        {
            var suppliers = await supplierService.GetAllSuppliersAsync();
            return Ok(suppliers);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving suppliers" });
        }
    }

    /// <summary>
    /// Get suppliers with pagination
    /// </summary>
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetSuppliersPaged([FromQuery] PageRequest pageRequest)
    {
        try
        {
            var result = await supplierService.GetSuppliersPagedAsync(pageRequest);
            return Ok(result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving suppliers" });
        }
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
    {
        try
        {
            var supplier = await supplierService.GetSupplierByIdAsync(id);
            
            if (supplier == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found" });
            }

            return Ok(supplier);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving the supplier" });
        }
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierDto createSupplierDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var supplier = await supplierService.CreateSupplierAsync(createSupplierDto);
            
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the supplier" });
        }
    }

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] CreateSupplierDto updateSupplierDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var supplier = await supplierService.UpdateSupplierAsync(id, updateSupplierDto);
            
            if (supplier == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found" });
            }

            return Ok(supplier);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the supplier" });
        }
    }

    /// <summary>
    /// Delete a supplier (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        try
        {
            var success = await supplierService.DeleteSupplierAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the supplier" });
        }
    }
}