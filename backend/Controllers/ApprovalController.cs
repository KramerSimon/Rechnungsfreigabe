using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Services;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IInvoiceService _invoiceService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;

    public ApprovalController(
        IApprovalService approvalService,
        IInvoiceService invoiceService,
        INotificationService notificationService,
        ApplicationDbContext context)
    {
        _approvalService = approvalService;
        _invoiceService = invoiceService;
        _notificationService = notificationService;
        _context = context;
    }

    /// <summary>
    /// Get pending approvals for current user
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<ApprovalWorkflowDto>>> GetPendingApprovals()
    {
        try
        {
            var userId = GetCurrentUserId();
            var approvals = await _approvalService.GetPendingApprovalsAsync(userId);
            return Ok(approvals);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving pending approvals" });
        }
    }

    /// <summary>
    /// Approve an invoice
    /// </summary>
    [HttpPost("{approvalId}/approve")]
    public async Task<ActionResult> ApproveInvoice(int approvalId, [FromBody] ApprovalRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _approvalService.ApproveAsync(approvalId, userId, request.Comments);
            
            if (!success)
            {
                return NotFound(new { message = "Approval not found" });
            }

            // Get invoice ID for notification
            var invoiceId = await _approvalService.GetInvoiceIdFromApprovalAsync(approvalId);
            if (invoiceId > 0)
            {
                await _notificationService.NotifyApprovalStatusAsync(invoiceId, "approved");
            }

            return Ok(new { message = "Invoice approved successfully" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while approving the invoice" });
        }
    }

    /// <summary>
    /// Reject an invoice
    /// </summary>
    [HttpPost("{approvalId}/reject")]
    public async Task<ActionResult> RejectInvoice(int approvalId, [FromBody] ApprovalRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _approvalService.RejectAsync(approvalId, userId, request.Comments);
            
            if (!success)
            {
                return NotFound(new { message = "Approval not found" });
            }

            // Get invoice ID for notification
            var invoiceId = await _approvalService.GetInvoiceIdFromApprovalAsync(approvalId);
            if (invoiceId > 0)
            {
                await _notificationService.NotifyApprovalStatusAsync(invoiceId, "rejected");
            }

            return Ok(new { message = "Invoice rejected successfully" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while rejecting the invoice" });
        }
    }

    /// <summary>
    /// Get approval rules
    /// </summary>
    [HttpGet("rules")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<ApprovalRule>>> GetApprovalRules()
    {
        try
        {
            var rules = await _approvalService.GetActiveRulesAsync();
            return Ok(rules);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetApprovalRules: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred while retrieving approval rules", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all approval workflows (admin only)
    /// </summary>
    [HttpGet("workflows")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<ApprovalWorkflowDto>>> GetApprovalWorkflows()
    {
        try
        {
            var workflows = await _approvalService.GetAllWorkflowsAsync();
            return Ok(workflows);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while retrieving approval workflows" });
        }
    }

    /// <summary>
    /// Create an approval workflow (admin only)
    /// </summary>
    [HttpPost("workflows")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalWorkflowDto>> CreateApprovalWorkflow([FromBody] CreateApprovalWorkflowDto dto)
    {
        try
        {
            // Basic validation
            var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
            var approver = await _context.Users.FindAsync(dto.ApproverId);
            if (invoice == null || approver == null)
            {
                return BadRequest(new { message = "Invalid invoice or approver" });
            }

            // Parse Status if provided
            ApprovalStatus status = ApprovalStatus.Pending;
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                if (!Enum.TryParse<ApprovalStatus>(dto.Status, true, out status))
                {
                    return BadRequest(new { message = "Invalid status value" });
                }
            }

            var workflow = new ApprovalWorkflow
            {
                InvoiceId = dto.InvoiceId,
                RuleId = dto.RuleId,
                StepNumber = dto.StepNumber,
                ApproverId = dto.ApproverId,
                ApprovalLevel = dto.ApprovalLevel,
                Status = status,
                Comments = dto.Comments,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApprovalWorkflows.Add(workflow);
            await _context.SaveChangesAsync();

            // Send notification to approver if workflow is pending (duplicate-safe)
            if (status == ApprovalStatus.Pending)
            {
                await _notificationService.EnsureApprovalNotificationForApproverAsync(dto.InvoiceId, dto.ApproverId);
            }

            var result = new ApprovalWorkflowDto
            {
                Id = workflow.Id,
                InvoiceId = workflow.InvoiceId,
                RuleId = workflow.RuleId,
                StepNumber = workflow.StepNumber,
                ApproverId = workflow.ApproverId,
                ApproverName = approver.FirstName + " " + approver.LastName,
                ApprovalLevel = workflow.ApprovalLevel,
                Status = workflow.Status.ToString(),
                Comments = workflow.Comments,
                ApprovedAt = workflow.ApprovedAt,
                CreatedAt = workflow.CreatedAt
            };

            return CreatedAtAction(nameof(GetApprovalWorkflows), new { id = result.Id }, result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the approval workflow" });
        }
    }

    /// <summary>
    /// Update an approval workflow (admin only)
    /// </summary>
    [HttpPut("workflows/{workflowId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalWorkflowDto>> UpdateApprovalWorkflow(int workflowId, [FromBody] UpdateApprovalWorkflowDto dto)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(workflowId);
            if (workflow == null)
            {
                return NotFound(new { message = "Approval workflow not found" });
            }

            if (dto.InvoiceId.HasValue)
            {
                var invoiceExists = await _context.Invoices.FindAsync(dto.InvoiceId.Value) != null;
                if (!invoiceExists) return BadRequest(new { message = "Invalid invoice" });
                workflow.InvoiceId = dto.InvoiceId.Value;
            }

            if (dto.RuleId.HasValue)
            {
                workflow.RuleId = dto.RuleId.Value;
            }

            if (dto.StepNumber.HasValue)
            {
                workflow.StepNumber = dto.StepNumber.Value;
            }

            if (dto.ApproverId.HasValue)
            {
                var approverExists = await _context.Users.FindAsync(dto.ApproverId.Value) != null;
                if (!approverExists) return BadRequest(new { message = "Invalid approver" });
                workflow.ApproverId = dto.ApproverId.Value;
            }

            if (dto.ApprovalLevel.HasValue)
            {
                workflow.ApprovalLevel = dto.ApprovalLevel.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                if (Enum.TryParse<ApprovalStatus>(dto.Status, true, out var status))
                {
                    workflow.Status = status;
                }
                else
                {
                    return BadRequest(new { message = "Invalid status" });
                }
            }

            if (dto.Comments != null)
            {
                workflow.Comments = dto.Comments;
            }

            _context.ApprovalWorkflows.Update(workflow);
            await _context.SaveChangesAsync();

            var approver = await _context.Users.FindAsync(workflow.ApproverId);
            var result = new ApprovalWorkflowDto
            {
                Id = workflow.Id,
                InvoiceId = workflow.InvoiceId,
                RuleId = workflow.RuleId,
                StepNumber = workflow.StepNumber,
                ApproverId = workflow.ApproverId,
                ApproverName = approver != null ? approver.FirstName + " " + approver.LastName : string.Empty,
                ApprovalLevel = workflow.ApprovalLevel,
                Status = workflow.Status.ToString(),
                Comments = workflow.Comments,
                ApprovedAt = workflow.ApprovedAt,
                CreatedAt = workflow.CreatedAt
            };

            return Ok(result);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the approval workflow" });
        }
    }

    /// <summary>
    /// Delete an approval workflow (admin only)
    /// </summary>
    [HttpDelete("workflows/{workflowId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteApprovalWorkflow(int workflowId)
    {
        try
        {
            var success = await _approvalService.DeleteWorkflowAsync(workflowId);
            if (!success)
            {
                return NotFound(new { message = "Approval workflow not found" });
            }

            return Ok(new { message = "Approval workflow deleted successfully" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the approval workflow" });
        }
    }

    /// <summary>
    /// Create approval rule
    /// </summary>
    [HttpPost("rules")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalRule>> CreateApprovalRule([FromBody] CreateApprovalRuleDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Missing or invalid user context" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                return Unauthorized(new { message = "User does not exist or is inactive" });
            }

            // Validate JSON payloads early to avoid server errors
            var conditionsJson = string.IsNullOrWhiteSpace(dto.Conditions) ? "[]" : dto.Conditions;
            var actionsJson = string.IsNullOrWhiteSpace(dto.Actions) ? "[]" : dto.Actions;

            try
            {
                System.Text.Json.JsonSerializer.Deserialize<RechnungsfreigabeAPI.Services.RuleCondition[]>(conditionsJson);
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON for conditions" });
            }

            try
            {
                System.Text.Json.JsonSerializer.Deserialize<RechnungsfreigabeAPI.Services.RuleAction[]>(actionsJson);
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON for actions" });
            }

            var rule = new ApprovalRule
            {
                Name = dto.Name,
                Description = dto.Description,
                RuleType = dto.RuleType,
                Priority = dto.Priority ?? 10,
                Conditions = conditionsJson,
                Actions = actionsJson,
                CreatedBy = userId
            };

            var createdRule = await _approvalService.CreateRuleAsync(rule);
            return CreatedAtAction(nameof(GetApprovalRules), new { id = createdRule.Id }, createdRule);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while creating the approval rule" });
        }
    }

    /// <summary>
    /// Update approval rule
    /// </summary>
    [HttpPut("rules/{ruleId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalRule>> UpdateApprovalRule(int ruleId, [FromBody] UpdateApprovalRuleDto dto)
    {
        try
        {
            var rule = await _context.ApprovalRules.FindAsync(ruleId);
            if (rule == null)
            {
                return NotFound(new { message = "Approval rule not found" });
            }

            if (!string.IsNullOrEmpty(dto.Name))
                rule.Name = dto.Name;
            if (dto.Description != null)
                rule.Description = dto.Description;
            if (dto.RuleType.HasValue)
                rule.RuleType = dto.RuleType.Value;
            if (dto.Priority.HasValue)
                rule.Priority = dto.Priority.Value;
            if (dto.Conditions != null)
                rule.Conditions = dto.Conditions;
            if (dto.Actions != null)
                rule.Actions = dto.Actions;
            if (dto.IsActive.HasValue)
                rule.IsActive = dto.IsActive.Value;
            
            rule.UpdatedAt = DateTime.UtcNow;

            _context.ApprovalRules.Update(rule);
            await _context.SaveChangesAsync();

            return Ok(rule);
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while updating the approval rule" });
        }
    }

    /// <summary>
    /// Delete approval rule
    /// </summary>
    [HttpDelete("rules/{ruleId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteApprovalRule(int ruleId)
    {
        try
        {
            var success = await _approvalService.DeleteRuleAsync(ruleId);
            if (!success)
            {
                return NotFound(new { message = "Approval rule not found" });
            }

            return Ok(new { message = "Approval rule deleted successfully" });
        }
        catch (Exception)
        {
            
            return StatusCode(500, new { message = "An error occurred while deleting the approval rule" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Debug endpoint to check current user auth status
    /// </summary>
    [HttpGet("debug/auth")]
    public IActionResult DebugAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var userId = GetCurrentUserId();
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        
        return Ok(new
        {
            IsAuthenticated = isAuthenticated,
            UserId = userId,
            Roles = roles,
            AllClaims = claims
        });
    }
}

// DTOs
public class ApprovalRequestDto
{
    public string? Comments { get; set; }
}

public class CreateApprovalRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RuleType RuleType { get; set; } = RuleType.Manual;
    public int? Priority { get; set; }
    public string? Conditions { get; set; }
    public string? Actions { get; set; }
    public int? SupplierId { get; set; }
    public string? CostCenterId { get; set; }
    public string? ProjectId { get; set; }
}

public class UpdateApprovalRuleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public RuleType? RuleType { get; set; }
    public int? Priority { get; set; }
    public string? Conditions { get; set; }
    public string? Actions { get; set; }
    public bool? IsActive { get; set; }
    public int? SupplierId { get; set; }
    public string? CostCenterId { get; set; }
    public string? ProjectId { get; set; }
}

public class CreateApprovalWorkflowDto
{
    public int InvoiceId { get; set; }
    public int? RuleId { get; set; }
    public int StepNumber { get; set; } = 1;
    public int ApproverId { get; set; }
    public int ApprovalLevel { get; set; } = 1;
    public string? Status { get; set; }
    public string? Comments { get; set; }
}

public class UpdateApprovalWorkflowDto
{
    public int? InvoiceId { get; set; }
    public int? RuleId { get; set; }
    public int? StepNumber { get; set; }
    public int? ApproverId { get; set; }
    public int? ApprovalLevel { get; set; }
    public string? Status { get; set; }
    public string? Comments { get; set; }
}
