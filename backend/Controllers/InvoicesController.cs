using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Interfaces.Services.Implementations;
using System.Security.Claims;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService invoiceService;
    private readonly IUserService userService;
    private readonly IApprovalService approvalService;

    public InvoicesController(IInvoiceService invoiceService, IUserService userService, IApprovalService approvalService)
    {
        this.invoiceService = invoiceService;
        this.userService = userService;
        this.approvalService = approvalService;
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
            var userPermissions = await userService.GetUserPermissionsAsync(userId);
            var result = await invoiceService.GetInvoicesPagedAsync(pageRequest, userId, userPermissions);
            return Ok(result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving invoices" });
        }
    }

    /// <summary>
    /// Get all invoices without user filtering (for admin master data)
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetAllInvoices([FromQuery] PageRequest pageRequest)
    {
        try
        {
            var result = await invoiceService.GetAllInvoicesPagedAsync(pageRequest);
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
            var userPermissions = await userService.GetUserPermissionsAsync(userId);
            var invoice = await invoiceService.GetInvoiceByIdAsync(id, userId, userPermissions);
            
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
            var invoice = await invoiceService.CreateInvoiceAsync(createInvoiceDto, userId);
            
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
            var invoice = await invoiceService.UpdateInvoiceAsync(id, updateInvoiceDto, userId);
            
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
            var success = await invoiceService.DeleteInvoiceAsync(id);
            
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
            var stats = await invoiceService.GetDashboardStatsAsync();
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
            var invoices = await invoiceService.GetPendingApprovalsAsync(userId);
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

            var userPermissions = await userService.GetUserPermissionsAsync(userId);
            var invoice = await invoiceService.GetInvoiceByIdAsync(id, userId, userPermissions);
            if (invoice == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }
            if (string.IsNullOrWhiteSpace(invoice.CostCenterId) || string.IsNullOrWhiteSpace(invoice.ProjectId))
            {
                return BadRequest(new { message = "Rechnung kann nicht freigegeben werden: fehlende Daten (Kostenstelle und/oder Projekt)." });
            }

            var success = await invoiceService.ApproveInvoiceAsync(id, userId, approveDto);
            
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
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new { message = "Status is required" });
            }

            var userId = GetCurrentUserId();
            if (string.Equals(status, RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt, StringComparison.OrdinalIgnoreCase))
            {
                var permissions = await userService.GetUserPermissionsAsync(userId);
                if (!permissions.Contains("payments.process"))
                {
                    return Forbid();
                }
            }
            var invoice = await invoiceService.UpdateInvoiceStatusAsync(id, status, userId);
            
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

    /// <summary>
    /// Manually create an approval workflow for an invoice when none exists.
    /// Intended for accounting task handling when no rule matched automatically.
    /// </summary>
    [HttpPost("{id}/workflows/manual")]
    public async Task<IActionResult> CreateManualWorkflow(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userPermissions = await userService.GetUserPermissionsAsync(userId);

            var canCreateWorkflow = userPermissions.Contains("invoices.edit") ||
                                    userPermissions.Contains("invoices.approve") ||
                                    userPermissions.Contains("invoices.approve_cost_center") ||
                                    userPermissions.Contains("dashboards.view_admin") ||
                                    userPermissions.Contains("dashboards.view_all");

            if (!canCreateWorkflow)
            {
                return Forbid();
            }

            var invoice = await invoiceService.GetInvoiceByIdAsync(id, userId, userPermissions);
            if (invoice == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            if (!invoice.RequiresApproval)
            {
                return BadRequest(new { message = "Invoice does not require approval." });
            }

            var existingWorkflows = (await approvalService.GetAllWorkflowsAsync())
                .Where(w => w.InvoiceId == id)
                .ToList();

            if (existingWorkflows.Any())
            {
                return BadRequest(new { message = "A workflow already exists for this invoice." });
            }

            await approvalService.CreateApprovalWorkflowAsync(id);

            var createdWorkflows = (await approvalService.GetAllWorkflowsAsync())
                .Where(w => w.InvoiceId == id)
                .ToList();

            if (!createdWorkflows.Any())
            {
                return BadRequest(new { message = "No workflow could be created. Check approval rule and default approver configuration." });
            }

            return Ok(new
            {
                message = "Workflow created successfully.",
                workflowCount = createdWorkflows.Count
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while creating the workflow." });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}