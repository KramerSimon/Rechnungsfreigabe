using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Text.Json;

namespace RechnungsfreigabeAPI.Services;

public interface IApprovalService
{
    Task CreateApprovalWorkflowAsync(int invoiceId);
    Task<bool> EvaluateApprovalRulesAsync(int invoiceId);
    Task<IEnumerable<ApprovalRule>> GetActiveRulesAsync();
    Task<IEnumerable<ApprovalWorkflowDto>> GetAllWorkflowsAsync();
    Task<ApprovalRule> CreateRuleAsync(ApprovalRule rule);
    Task<bool> DeleteRuleAsync(int ruleId);
    Task<bool> DeleteWorkflowAsync(int workflowId);
    Task<IEnumerable<ApprovalWorkflowDto>> GetPendingApprovalsAsync(int userId);
    Task<bool> ApproveAsync(int approvalId, int userId, string? comments);
    Task<bool> RejectAsync(int approvalId, int userId, string? comments);
    Task<int> GetInvoiceIdFromApprovalAsync(int approvalId);
}

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    public ApprovalService(ApplicationDbContext context)
    {
        _context = context;
        }

    public async Task CreateApprovalWorkflowAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.CostCenter)
                .ThenInclude(cc => cc!.Manager)
                .Include(i => i.Project)
                .ThenInclude(p => p!.ProjectManager)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                
                return;
            }

            var matchedRule = await FindMatchingRuleAsync(invoice);
            
            if (matchedRule != null)
            {
                
                await ProcessRuleActionsAsync(invoice, matchedRule);
            }
            else
            {
                
                // Default approval workflow
                await CreateDefaultApprovalWorkflowAsync(invoice);
            }
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> EvaluateApprovalRulesAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.CostCenter)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return false;

            var rules = await GetActiveRulesAsync();
            
            foreach (var rule in rules.OrderBy(r => r.Priority))
            {
                if (await EvaluateRuleConditionsAsync(invoice, rule))
                {
                    await ProcessRuleActionsAsync(invoice, rule);
                    return true;
                }
            }

            return false;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    public async Task<IEnumerable<ApprovalRule>> GetActiveRulesAsync()
    {
        return await _context.ApprovalRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<ApprovalRule> CreateRuleAsync(ApprovalRule rule)
    {
        try
        {
            _context.ApprovalRules.Add(rule);
            await _context.SaveChangesAsync();

            return rule;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<bool> DeleteRuleAsync(int ruleId)
    {
        try
        {
            var rule = await _context.ApprovalRules.FindAsync(ruleId);
            if (rule == null) return false;

            _context.ApprovalRules.Remove(rule);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    private async Task<ApprovalRule?> FindMatchingRuleAsync(Invoice invoice)
    {
        var rules = await GetActiveRulesAsync();

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {

            if (await EvaluateRuleConditionsAsync(invoice, rule))
            {
                
                return rule;
            }
        }

        return null;
    }

    private async Task<bool> EvaluateRuleConditionsAsync(Invoice invoice, ApprovalRule rule)
    {
        try
        {

            var conditions = JsonSerializer.Deserialize<RuleCondition[]>(rule.Conditions);

            if (conditions == null || !conditions.Any())
            {
                
                return true; // No conditions means rule applies to all
            }

            foreach (var condition in conditions)
            {
                if (!await EvaluateConditionAsync(invoice, condition))
                    return false;
            }

            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    private Task<bool> EvaluateConditionAsync(Invoice invoice, RuleCondition condition)
    {
        var fieldLower = condition.Field.ToLower();
        var result = fieldLower switch
        {
            "total_amount" or "amount" => EvaluateNumericCondition(invoice.TotalAmount, condition),
            "cost_center_id" => EvaluateStringCondition(invoice.CostCenterId, condition),
            "project_id" => EvaluateStringCondition(invoice.ProjectId, condition),
            "supplier_id" => EvaluateNumericCondition(invoice.SupplierId, condition),
            "currency" => EvaluateStringCondition(invoice.Currency, condition),
            _ => false
        };

        return Task.FromResult(result);
    }

    private bool EvaluateNumericCondition(decimal value, RuleCondition condition)
    {
        if (!decimal.TryParse(condition.Value, out var conditionValue))
            return false;

        return condition.Operator switch
        {
            "=" => value == conditionValue,
            ">" => value > conditionValue,
            "<" => value < conditionValue,
            ">=" => value >= conditionValue,
            "<=" => value <= conditionValue,
            "!=" => value != conditionValue,
            _ => false
        };
    }

    private bool EvaluateNumericCondition(int value, RuleCondition condition)
    {
        if (!int.TryParse(condition.Value, out var conditionValue))
            return false;

        return condition.Operator switch
        {
            "=" => value == conditionValue,
            ">" => value > conditionValue,
            "<" => value < conditionValue,
            ">=" => value >= conditionValue,
            "<=" => value <= conditionValue,
            "!=" => value != conditionValue,
            _ => false
        };
    }

    private bool EvaluateStringCondition(string? value, RuleCondition condition)
    {
        return condition.Operator switch
        {
            "=" => value == condition.Value,
            "!=" => value != condition.Value,
            "contains" => value?.Contains(condition.Value) ?? false,
            "starts_with" => value?.StartsWith(condition.Value) ?? false,
            "ends_with" => value?.EndsWith(condition.Value) ?? false,
            _ => false
        };
    }

    private async Task ProcessRuleActionsAsync(Invoice invoice, ApprovalRule rule)
    {
        try
        {
            var actions = JsonSerializer.Deserialize<RuleAction[]>(rule.Actions);
            
            if (actions == null) return;

            foreach (var action in actions)
            {
                await ProcessRuleActionAsync(invoice, rule, action);
            }
        }
        catch (Exception)
        {
            
        }
    }

    private async Task ProcessRuleActionAsync(Invoice invoice, ApprovalRule rule, RuleAction action)
    {
        switch (action.Type.ToLower())
        {
            case "auto_approve":
                await AutoApproveInvoiceAsync(invoice);
                break;
            case "require_approval":
                await CreateApprovalWorkflowStepsAsync(invoice, rule, action);
                break;
            case "set_status":
                if (Enum.TryParse<InvoiceStatus>(action.Value, out var status))
                {
                    invoice.Status = status;
                    await _context.SaveChangesAsync();
                }
                break;
            case "assign_to":
                if (int.TryParse(action.Value, out var assignedUserId))
                {
                    await AssignInvoiceToUserAsync(invoice, rule, assignedUserId);
                }
                else
                {
                    
                }
                break;
        }
    }

    private async Task AutoApproveInvoiceAsync(Invoice invoice)
    {
        invoice.Status = InvoiceStatus.Freigegeben;
        invoice.AutoApproved = true;
        invoice.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
    }

    private async Task CreateApprovalWorkflowStepsAsync(Invoice invoice, ApprovalRule rule, RuleAction action)
    {
        var approvers = await GetApproversForActionAsync(invoice, action);
        
        if (!approvers.Any())
        {
            // Keine Approver gefunden - Auto-Approve
            Console.WriteLine($"[WARNING] No approvers found for invoice {invoice.Id}, auto-approving");
            invoice.Status = InvoiceStatus.Freigegeben;
            invoice.AutoApproved = true;
            await _context.SaveChangesAsync();
            return;
        }
        
        int stepNumber = 1;
        foreach (var approverId in approvers)
        {
            // Validiere dass der Approver existiert
            var approverExists = await _context.Users.AnyAsync(u => u.Id == approverId && u.IsActive);
            if (!approverExists)
            {
                Console.WriteLine($"[WARNING] Approver {approverId} not found or inactive, skipping");
                continue;
            }
            
            var workflow = new ApprovalWorkflow
            {
                InvoiceId = invoice.Id,
                RuleId = rule.Id,
                StepNumber = stepNumber++,
                ApproverId = approverId,
                ApprovalLevel = GetApprovalLevelForAction(action),
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApprovalWorkflows.Add(workflow);
        }

        invoice.Status = InvoiceStatus.Freigabe_Erforderlich;
        await _context.SaveChangesAsync();
    }

    private async Task AssignInvoiceToUserAsync(Invoice invoice, ApprovalRule rule, int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
        if (!userExists)
        {
            
            return;
        }

        var workflow = new ApprovalWorkflow
        {
            InvoiceId = invoice.Id,
            RuleId = rule.Id,
            StepNumber = 1,
            ApproverId = userId,
            ApprovalLevel = 1,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApprovalWorkflows.Add(workflow);

        invoice.Status = InvoiceStatus.Freigabe_Erforderlich;
        await _context.SaveChangesAsync();
    }

    private async Task CreateDefaultApprovalWorkflowAsync(Invoice invoice)
    {
        var approvers = new List<int>();

        // Add cost center manager if available
        if (invoice.CostCenter?.ManagerId.HasValue == true)
        {
            var managerExists = await _context.Users.AnyAsync(u => u.Id == invoice.CostCenter.ManagerId.Value && u.IsActive);
            if (managerExists)
            {
                approvers.Add(invoice.CostCenter.ManagerId.Value);
            }
        }

        // Add project manager if available and different from cost center manager
        if (invoice.Project?.ProjectManagerId.HasValue == true && 
            invoice.Project.ProjectManagerId != invoice.CostCenter?.ManagerId)
        {
            var pmExists = await _context.Users.AnyAsync(u => u.Id == invoice.Project.ProjectManagerId.Value && u.IsActive);
            if (pmExists)
            {
                approvers.Add(invoice.Project.ProjectManagerId.Value);
            }
        }

        // If no specific approvers, find users with approval permissions
        if (!approvers.Any())
        {
            var approverUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => 
                    ur.Role.Name == "Freigeber" || ur.Role.Name == "Manager"))
                .Take(1)
                .Select(u => u.Id)
                .ToListAsync();

            approvers.AddRange(approverUsers);
        }

        int stepNumber = 1;
        foreach (var approverId in approvers)
        {
            var workflow = new ApprovalWorkflow
            {
                InvoiceId = invoice.Id,
                StepNumber = stepNumber++,
                ApproverId = approverId,
                ApprovalLevel = 1,
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApprovalWorkflows.Add(workflow);
        }

        if (approvers.Any())
        {
            invoice.Status = InvoiceStatus.Freigabe_Erforderlich;
        }
        else
        {
            // No approvers found, auto-approve
            Console.WriteLine($"[WARNING] No approvers found for invoice {invoice.Id}, auto-approving");
            invoice.Status = InvoiceStatus.Freigegeben;
            invoice.AutoApproved = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<int[]> GetApproversForActionAsync(Invoice invoice, RuleAction action)
    {
        var approvers = new List<int>();

        switch (action.Value.ToLower())
        {
            case "manager":
                if (invoice.CostCenter?.ManagerId.HasValue == true)
                    approvers.Add(invoice.CostCenter.ManagerId.Value);
                break;
            case "project_manager":
                if (invoice.Project?.ProjectManagerId.HasValue == true)
                    approvers.Add(invoice.Project.ProjectManagerId.Value);
                break;
            case "double":
                if (invoice.CostCenter?.ManagerId.HasValue == true)
                    approvers.Add(invoice.CostCenter.ManagerId.Value);
                // Add admin user for second approval
                var adminUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IsActive && 
                        u.UserRoles.Any(ur => ur.Role.Name == "Administrator"));
                if (adminUser != null)
                    approvers.Add(adminUser.Id);
                break;
            default:
                // Standard approval - cost center manager
                if (invoice.CostCenter?.ManagerId.HasValue == true)
                    approvers.Add(invoice.CostCenter.ManagerId.Value);
                break;
        }

        return approvers.ToArray();
    }

    private int GetApprovalLevelForAction(RuleAction action)
    {
        return action.Value.ToLower() switch
        {
            "double" => 2,
            _ => 1
        };
    }

    public async Task<IEnumerable<ApprovalWorkflowDto>> GetPendingApprovalsAsync(int userId)
    {
        try
        {
            var workflows = await _context.ApprovalWorkflows
                .Include(w => w.Invoice)
                .Include(w => w.Approver)
                .Include(w => w.Rule)
                .Where(w => w.ApproverId == userId && w.Status == ApprovalStatus.Pending)
                .OrderBy(w => w.CreatedAt)
                .Select(w => new ApprovalWorkflowDto
                {
                    Id = w.Id,
                    InvoiceId = w.InvoiceId,
                    InvoiceNumber = w.Invoice.InvoiceNumber,
                    RuleId = w.RuleId,
                    RuleName = w.Rule != null ? w.Rule.Name : null,
                    StepNumber = w.StepNumber,
                    Approver = new UserDto
                    {
                        Id = w.Approver.Id,
                        Username = w.Approver.Username,
                        Email = w.Approver.Email,
                        FirstName = w.Approver.FirstName,
                        LastName = w.Approver.LastName
                    },
                    ApprovalLevel = w.ApprovalLevel,
                    Status = w.Status.ToString(),
                    Comments = w.Comments,
                    ApprovedAt = w.ApprovedAt,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return workflows;
        }
        catch (Exception)
        {
            
            return new List<ApprovalWorkflowDto>();
        }
    }

    public async Task<bool> ApproveAsync(int approvalId, int userId, string? comments)
    {
        try
        {
            var approval = await _context.ApprovalWorkflows
                .Include(w => w.Invoice)
                .FirstOrDefaultAsync(w => w.Id == approvalId);

            if (approval == null || approval.Status != ApprovalStatus.Pending)
                return false;

            if (approval.ApproverId != userId)
            {
                
                return false;
            }

            approval.Status = ApprovalStatus.Approved;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Comments = comments;

            // Check if all approvals are done
            var invoice = approval.Invoice;
            var pendingApprovals = await _context.ApprovalWorkflows
                .Where(w => w.InvoiceId == invoice.Id && w.Status == ApprovalStatus.Pending && w.Id != approvalId)
                .CountAsync();

            if (pendingApprovals == 0)
            {
                // All approvals done, set invoice as approved
                invoice.Status = InvoiceStatus.Freigegeben;
                
            }

            await _context.SaveChangesAsync();
            
            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    public async Task<bool> RejectAsync(int approvalId, int userId, string? comments)
    {
        try
        {
            var approval = await _context.ApprovalWorkflows
                .Include(w => w.Invoice)
                .FirstOrDefaultAsync(w => w.Id == approvalId);

            if (approval == null || approval.Status != ApprovalStatus.Pending)
                return false;

            if (approval.ApproverId != userId)
            {
                
                return false;
            }

            approval.Status = ApprovalStatus.Rejected;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Comments = comments;

            // Mark invoice as rejected
            var invoice = approval.Invoice;
            invoice.Status = InvoiceStatus.Abgelehnt;

            // Reject all pending approvals for this invoice
            var pendingApprovals = await _context.ApprovalWorkflows
                .Where(w => w.InvoiceId == invoice.Id && w.Status == ApprovalStatus.Pending)
                .ToListAsync();

            foreach (var pending in pendingApprovals)
            {
                pending.Status = ApprovalStatus.Rejected;
            }

            await _context.SaveChangesAsync();
            
            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }

    public async Task<int> GetInvoiceIdFromApprovalAsync(int approvalId)
    {
        try
        {
            var approval = await _context.ApprovalWorkflows
                .FirstOrDefaultAsync(w => w.Id == approvalId);

            return approval?.InvoiceId ?? 0;
        }
        catch (Exception)
        {
            
            return 0;
        }
    }

    public async Task<IEnumerable<ApprovalWorkflowDto>> GetAllWorkflowsAsync()
    {
        try
        {
            var workflows = await _context.ApprovalWorkflows
                .Include(w => w.Approver)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new ApprovalWorkflowDto
                {
                    Id = w.Id,
                    InvoiceId = w.InvoiceId,
                    StepNumber = w.StepNumber,
                    ApproverName = $"{w.Approver.FirstName} {w.Approver.LastName}",
                    Status = w.Status.ToString(),
                    Comments = w.Comments,
                    ApprovedAt = w.ApprovedAt,
                    CreatedAt = w.CreatedAt,
                    ApproverId = w.ApproverId,
                    ApprovalLevel = w.ApprovalLevel
                })
                .ToListAsync();

            return workflows;
        }
        catch (Exception)
        {
            
            return Enumerable.Empty<ApprovalWorkflowDto>();
        }
    }

    public async Task<bool> DeleteWorkflowAsync(int workflowId)
    {
        try
        {
            var workflow = await _context.ApprovalWorkflows.FindAsync(workflowId);
            if (workflow == null) return false;

            _context.ApprovalWorkflows.Remove(workflow);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }
}

// Helper classes for JSON deserialization
public class RuleCondition
{
    [System.Text.Json.Serialization.JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("operator")]
    public string Operator { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("logicalOperator")]
    public string? LogicalOperator { get; set; }
}

public class RuleAction
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
}
