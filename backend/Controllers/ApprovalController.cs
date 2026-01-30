using Microsoft.AspNetCore.Mvc;
using RechnungsfreigabeAPI.Interfaces.Services.Implementations;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Controllers;

[ApiController]
[Route("api/v1/approvals")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService approvalService;
    private readonly IInvoiceService invoiceService;
    private readonly INotificationService notificationService;
    private readonly ApplicationDbContext context;
    private readonly ILogger<ApprovalController> logger;

    public ApprovalController(
        IApprovalService approvalService,
        IInvoiceService invoiceService,
        INotificationService notificationService,
        ApplicationDbContext context,
        ILogger<ApprovalController> logger)
    {
        this.approvalService = approvalService;
        this.invoiceService = invoiceService;
        this.notificationService = notificationService;
        this.context = context;
        this.logger = logger;
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
            var approvals = await approvalService.GetPendingApprovalsAsync(userId);
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
            var success = await approvalService.ApproveAsync(approvalId, userId, request.Comments);
            
            if (!success)
            {
                return NotFound(new { message = "Approval not found" });
            }

            // Get invoice ID for notification
            var invoiceId = await approvalService.GetInvoiceIdFromApprovalAsync(approvalId);
            if (invoiceId > 0)
            {
                await notificationService.NotifyApprovalStatusAsync(invoiceId, "approved");
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
            var success = await approvalService.RejectAsync(approvalId, userId, request.Comments);
            
            if (!success)
            {
                return NotFound(new { message = "Approval not found" });
            }

            // Get invoice ID for notification
            var invoiceId = await approvalService.GetInvoiceIdFromApprovalAsync(approvalId);
            if (invoiceId > 0)
            {
                await notificationService.NotifyApprovalStatusAsync(invoiceId, "rejected");
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
            var rules = await approvalService.GetActiveRulesAsync();
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
    /// Get approval rule by id
    /// </summary>
    [HttpGet("rules/{ruleId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApprovalRule>> GetApprovalRule(int ruleId)
    {
        try
        {
            var rule = await context.ApprovalRules
                .Include(r => r.Conditions.OrderBy(c => c.ConditionOrder))
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.Role)
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null)
            {
                return NotFound(new { message = "Approval rule not found" });
            }

            return Ok(rule);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetApprovalRule: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred while retrieving approval rule", error = ex.Message });
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
            var workflows = await approvalService.GetAllWorkflowsAsync();
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
            var invoice = await context.Invoices.FindAsync(dto.InvoiceId);
            var approver = await context.Users.FindAsync(dto.ApproverId);
            if (invoice == null || approver == null)
            {
                return BadRequest(new { message = "Invalid invoice or approver" });
            }

            // Parse Status if provided - use status code lookup instead of enum
            // Status is now handled through the Status relationship with the database
            int? statusId = null;
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                statusId = await context.Statuses
                    .Where(s => s.Code == dto.Status && s.EntityType == EntityTypes.ApprovalWorkflow)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();
                if (!statusId.HasValue)
                {
                    return BadRequest(new { message = "Invalid status value" });
                }
            }
            else
            {
                // Default to Pending status
                statusId = await context.Statuses
                    .Where(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending && s.EntityType == EntityTypes.ApprovalWorkflow)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();
            }

            var workflow = new ApprovalWorkflow
            {
                InvoiceId = dto.InvoiceId,
                RuleId = dto.RuleId,
                StepNumber = dto.StepNumber,
                ApproverId = dto.ApproverId,
                ApprovalLevel = dto.ApprovalLevel,
                StatusId = statusId,
                Comments = dto.Comments,
                CreatedAt = DateTime.UtcNow
            };

            context.ApprovalWorkflows.Add(workflow);
            await context.SaveChangesAsync();

            // Send notification to approver if workflow is pending (duplicate-safe)
            var pendingStatus = await context.Statuses
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending && s.EntityType == EntityTypes.ApprovalWorkflow);
            if (statusId == pendingStatus?.Id)
            {
                await notificationService.EnsureApprovalNotificationForApproverAsync(dto.InvoiceId, dto.ApproverId);
            }

            var result = new ApprovalWorkflowDto
            {
                Id = workflow.Id,
                InvoiceId = workflow.InvoiceId,
                RuleId = workflow.RuleId,
                StepNumber = workflow.StepNumber,
                ApproverId = workflow.ApproverId,
                ApproverName = approver != null ? approver.FirstName + " " + approver.LastName : string.Empty,
                ApprovalLevel = workflow.ApprovalLevel,
                Status = workflow.Status?.ToString() ?? string.Empty,
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
            var workflow = await context.ApprovalWorkflows.FindAsync(workflowId);
            if (workflow == null)
            {
                return NotFound(new { message = "Approval workflow not found" });
            }

            if (dto.InvoiceId.HasValue)
            {
                var invoiceExists = await context.Invoices.FindAsync(dto.InvoiceId.Value) != null;
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
                var approverExists = await context.Users.FindAsync(dto.ApproverId.Value) != null;
                if (!approverExists) return BadRequest(new { message = "Invalid approver" });
                workflow.ApproverId = dto.ApproverId.Value;
            }

            if (dto.ApprovalLevel.HasValue)
            {
                workflow.ApprovalLevel = dto.ApprovalLevel.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var statusId = await context.Statuses
                    .Where(s => s.Code == dto.Status && s.EntityType == EntityTypes.ApprovalWorkflow)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();
                if (statusId.HasValue)
                {
                    workflow.StatusId = statusId.Value;
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

            context.ApprovalWorkflows.Update(workflow);
            await context.SaveChangesAsync();

            var approver = await context.Users.FindAsync(workflow.ApproverId);
            var result = new ApprovalWorkflowDto
            {
                Id = workflow.Id,
                InvoiceId = workflow.InvoiceId,
                RuleId = workflow.RuleId,
                StepNumber = workflow.StepNumber,
                ApproverId = workflow.ApproverId,
                ApproverName = approver != null ? approver.FirstName + " " + approver.LastName : string.Empty,
                ApprovalLevel = workflow.ApprovalLevel,
                Status = workflow.Status?.ToString() ?? string.Empty,
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
            var success = await approvalService.DeleteWorkflowAsync(workflowId);
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

            var userExists = await context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                return Unauthorized(new { message = "User does not exist or is inactive" });
            }

            var rule = new ApprovalRule
            {
                Name = dto.Name,
                Description = dto.Description,
                RuleType = dto.RuleType,
                Priority = dto.Priority ?? 10,
                CreatedBy = userId
            };

            // Add conditions
            if (dto.Conditions != null && dto.Conditions.Any())
            {
                int order = 1;
                foreach (var condDto in dto.Conditions)
                {
                    rule.Conditions.Add(new ApprovalRuleCondition
                    {
                        Field = condDto.Field,
                        Operator = condDto.Operator,
                        Value = condDto.Value,
                        LogicalOperator = condDto.LogicalOperator ?? "AND",
                        ConditionOrder = order++
                    });
                }
            }

            // Add actions
            if (dto.Actions != null && dto.Actions.Any())
            {
                int actionOrder = 1;
                foreach (var actDto in dto.Actions)
                {
                    var action = new ApprovalRuleAction
                    {
                        ActionType = actDto.ActionType,
                        ActionValue = actDto.ActionValue,
                        Description = actDto.Description,
                        ActionOrder = actionOrder++
                    };

                    // Add stages if present
                    if (actDto.Stages != null && actDto.Stages.Any())
                    {
                        foreach (var stageDto in actDto.Stages)
                        {
                            action.Stages.Add(new ApprovalRuleStage
                            {
                                StepNumber = stageDto.StepNumber,
                                ApprovalLevel = stageDto.ApprovalLevel,
                                RoleId = stageDto.RoleId,
                                UserId = stageDto.UserId
                            });
                        }
                    }

                    rule.Actions.Add(action);
                }
            }

            var createdRule = await approvalService.CreateRuleAsync(rule);
            
            // Reload with related entities
            var loadedRule = await context.ApprovalRules
                .Include(r => r.Conditions.OrderBy(c => c.ConditionOrder))
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.Role)
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == createdRule.Id);

            return CreatedAtAction(nameof(GetApprovalRules), new { id = createdRule.Id }, loadedRule);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating approval rule. Payload: {@Dto}", dto);
            return StatusCode(500, new
            {
                message = "An error occurred while creating the approval rule",
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
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
            var rule = await context.ApprovalRules
                .Include(r => r.Conditions)
                .Include(r => r.Actions)
                .ThenInclude(a => a.Stages)
                .FirstOrDefaultAsync(r => r.Id == ruleId);
                
            if (rule == null)
            {
                return NotFound(new { message = "Approval rule not found" });
            }

            // Update basic properties
            if (!string.IsNullOrEmpty(dto.Name))
                rule.Name = dto.Name;
            if (dto.Description != null)
                rule.Description = dto.Description;
            if (dto.RuleType.HasValue)
                rule.RuleType = dto.RuleType.Value;
            if (dto.Priority.HasValue)
                rule.Priority = dto.Priority.Value;
            if (dto.IsActive.HasValue)
                rule.IsActive = dto.IsActive.Value;

            // Update conditions
            if (dto.Conditions != null)
            {
                // Remove existing conditions
                context.ApprovalRuleConditions.RemoveRange(rule.Conditions);
                
                // Add new conditions
                int order = 1;
                foreach (var condDto in dto.Conditions)
                {
                    rule.Conditions.Add(new ApprovalRuleCondition
                    {
                        Field = condDto.Field,
                        Operator = condDto.Operator,
                        Value = condDto.Value,
                        LogicalOperator = condDto.LogicalOperator ?? "AND",
                        ConditionOrder = order++
                    });
                }
            }

            // Update actions
            if (dto.Actions != null)
            {
                // Remove existing actions (cascade will remove stages)
                context.ApprovalRuleActions.RemoveRange(rule.Actions);
                
                // Add new actions
                int actionOrder = 1;
                foreach (var actDto in dto.Actions)
                {
                    var action = new ApprovalRuleAction
                    {
                        ActionType = actDto.ActionType,
                        ActionValue = actDto.ActionValue,
                        Description = actDto.Description,
                        ActionOrder = actionOrder++
                    };

                    // Add stages if present
                    if (actDto.Stages != null && actDto.Stages.Any())
                    {
                        foreach (var stageDto in actDto.Stages)
                        {
                            action.Stages.Add(new ApprovalRuleStage
                            {
                                StepNumber = stageDto.StepNumber,
                                ApprovalLevel = stageDto.ApprovalLevel,
                                RoleId = stageDto.RoleId,
                                UserId = stageDto.UserId
                            });
                        }
                    }

                    rule.Actions.Add(action);
                }
            }
            
            rule.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Reload with all related entities
            var updatedRule = await context.ApprovalRules
                .Include(r => r.Conditions.OrderBy(c => c.ConditionOrder))
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.Role)
                .Include(r => r.Actions.OrderBy(a => a.ActionOrder))
                .ThenInclude(a => a.Stages.OrderBy(s => s.StepNumber))
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            return Ok(updatedRule);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating approval rule {RuleId}. Payload: {@Dto}", ruleId, dto);
            return StatusCode(500, new
            {
                message = "An error occurred while updating the approval rule",
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
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
            var success = await approvalService.DeleteRuleAsync(ruleId);
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

public class ApprovalRuleConditionDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? LogicalOperator { get; set; } = "AND";
}

public class ApprovalRuleActionDto
{
    public string ActionType { get; set; } = string.Empty;
    public string? ActionValue { get; set; }
    public string? Description { get; set; }
    public List<ApprovalRuleStageDto>? Stages { get; set; }
}

public class ApprovalRuleStageDto
{
    public int StepNumber { get; set; }
    public int ApprovalLevel { get; set; }
    public int? RoleId { get; set; }
    public int? UserId { get; set; }
}

public class CreateApprovalRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RuleType RuleType { get; set; } = RuleType.Manual;
    public int? Priority { get; set; }
    public List<ApprovalRuleConditionDto>? Conditions { get; set; }
    public List<ApprovalRuleActionDto>? Actions { get; set; }
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
    public List<ApprovalRuleConditionDto>? Conditions { get; set; }
    public List<ApprovalRuleActionDto>? Actions { get; set; }
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
