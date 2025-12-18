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
    private readonly ILogger<ApprovalController> _logger;
    private readonly ApplicationDbContext _context;

    public ApprovalController(
        IApprovalService approvalService,
        IInvoiceService invoiceService,
        INotificationService notificationService,
        ILogger<ApprovalController> logger,
        ApplicationDbContext context)
    {
        _approvalService = approvalService;
        _invoiceService = invoiceService;
        _notificationService = notificationService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
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

            _logger.LogInformation("Invoice approved by user {UserId}, approval {ApprovalId}", userId, approvalId);
            return Ok(new { message = "Invoice approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice");
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

            _logger.LogInformation("Invoice rejected by user {UserId}, approval {ApprovalId}", userId, approvalId);
            return Ok(new { message = "Invoice rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invoice");
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
            _logger.LogError(ex, "Error getting approval rules");
            return StatusCode(500, new { message = "An error occurred while retrieving approval rules" });
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval workflows");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval workflow");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating approval workflow");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting approval workflow {WorkflowId}", workflowId);
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
            var rule = new ApprovalRule
            {
                Name = dto.Name,
                Description = dto.Description,
                RuleType = dto.RuleType,
                Priority = dto.Priority ?? 10,
                Conditions = dto.Conditions ?? "[]",
                Actions = dto.Actions ?? "[]",
                CreatedBy = userId
            };

            var createdRule = await _approvalService.CreateRuleAsync(rule);
            return CreatedAtAction(nameof(GetApprovalRules), new { id = createdRule.Id }, createdRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval rule");
            return StatusCode(500, new { message = "An error occurred while creating the approval rule" });
        }
    }

    /// <summary>
    /// Update approval rule
    /// </summary>
    [HttpPut("rules/{ruleId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalRule>> UpdateApprovalRule(int ruleId, [FromBody] CreateApprovalRuleDto dto)
    {
        try
        {
            var rule = await _context.ApprovalRules.FindAsync(ruleId);
            if (rule == null)
            {
                return NotFound(new { message = "Approval rule not found" });
            }

            rule.Name = dto.Name;
            rule.Description = dto.Description;
            rule.RuleType = dto.RuleType;
            rule.Priority = dto.Priority ?? rule.Priority;
            rule.Conditions = dto.Conditions ?? rule.Conditions;
            rule.Actions = dto.Actions ?? rule.Actions;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.ApprovalRules.Update(rule);
            await _context.SaveChangesAsync();

            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating approval rule");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting approval rule");
            return StatusCode(500, new { message = "An error occurred while deleting the approval rule" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("nameid");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
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
