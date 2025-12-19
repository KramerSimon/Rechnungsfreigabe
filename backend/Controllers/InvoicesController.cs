using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Services;
using System.Security.Claims;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for testing
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUserService _userService;
    public InvoicesController(IInvoiceService invoiceService, IUserService userService)
    {
        _invoiceService = invoiceService;
        _userService = userService;
        }

    /// <summary>
    /// Get all invoices with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetInvoices([FromQuery] PageRequest pageRequest)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userPermissions = await _userService.GetUserPermissionsAsync(userId);
            var result = await _invoiceService.GetInvoicesPagedAsync(pageRequest, userId, userPermissions);
            return Ok(result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving invoices" });
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userPermissions = await _userService.GetUserPermissionsAsync(userId);
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, userId, userPermissions);
            
            if (invoice == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            return Ok(invoice);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving the invoice" });
        }
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice([FromBody] CreateInvoiceDto createInvoiceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);
            
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the invoice" });
        }
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto updateInvoiceDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, updateInvoiceDto, userId);
            
            if (invoice == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            return Ok(invoice);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the invoice" });
        }
    }

    /// <summary>
    /// Delete an invoice (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        try
        {
            var success = await _invoiceService.DeleteInvoiceAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the invoice" });
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        try
        {
            var stats = await _invoiceService.GetDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard statistics" });
        }
    }

    /// <summary>
    /// Get pending approvals for current user
    /// </summary>
    [HttpGet("pending-approvals")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetPendingApprovals()
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoices = await _invoiceService.GetPendingApprovalsAsync(userId);
            return Ok(invoices);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving pending approvals" });
        }
    }

    /// <summary>
    /// Approve or reject an invoice
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveInvoice(int id, [FromBody] ApproveInvoiceDto approveDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var success = await _invoiceService.ApproveInvoiceAsync(id, userId, approveDto);
            
            if (!success)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found or you don't have permission to approve it" });
            }

            return Ok(new { message = approveDto.Approved ? "Invoice approved successfully" : "Invoice rejected successfully" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while processing the approval" });
        }
    }

    /// <summary>
    /// Update invoice status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoiceStatus(int id, [FromBody] string status)
    {
        try
        {
            if (!Enum.TryParse<Models.InvoiceStatus>(status, out var invoiceStatus))
            {
                return BadRequest(new { message = "Invalid status value" });
            }

            var userId = GetCurrentUserId();
            var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, invoiceStatus, userId);
            
            if (invoice == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            return Ok(invoice);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the invoice status" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}