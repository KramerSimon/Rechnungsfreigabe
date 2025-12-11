using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.Models;
using System.Text.Json;

namespace RechnungsfreigabeAPI.Services;

public interface IApprovalService
{
    Task CreateApprovalWorkflowAsync(int invoiceId);
    Task<bool> EvaluateApprovalRulesAsync(int invoiceId);
    Task<IEnumerable<ApprovalRule>> GetActiveRulesAsync();
    Task<ApprovalRule> CreateRuleAsync(ApprovalRule rule);
    Task<bool> DeleteRuleAsync(int ruleId);
}

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(ApplicationDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateApprovalWorkflowAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.CostCenter)
                .ThenInclude(cc => cc.Manager)
                .Include(i => i.Project)
                .ThenInclude(p => p.ProjectManager)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found: {InvoiceId}", invoiceId);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval workflow for invoice: {InvoiceId}", invoiceId);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating approval rules for invoice: {InvoiceId}", invoiceId);
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

            _logger.LogInformation("Approval rule created: {RuleName}", rule.Name);
            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval rule: {RuleName}", rule.Name);
            throw;
        }
    }

    public async Task<bool> DeleteRuleAsync(int ruleId)
    {
        try
        {
            var rule = await _context.ApprovalRules.FindAsync(ruleId);
            if (rule == null) return false;

            rule.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Approval rule deactivated: {RuleId}", ruleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting approval rule: {RuleId}", ruleId);
            return false;
        }
    }

    private async Task<ApprovalRule?> FindMatchingRuleAsync(Invoice invoice)
    {
        var rules = await GetActiveRulesAsync();
        
        foreach (var rule in rules)
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
                return true; // No conditions means rule applies to all

            foreach (var condition in conditions)
            {
                if (!await EvaluateConditionAsync(invoice, condition))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule conditions for rule: {RuleId}", rule.Id);
            return false;
        }
    }

    private async Task<bool> EvaluateConditionAsync(Invoice invoice, RuleCondition condition)
    {
        return condition.Field.ToLower() switch
        {
            "total_amount" => EvaluateNumericCondition(invoice.TotalAmount, condition),
            "cost_center_id" => EvaluateStringCondition(invoice.CostCenterId, condition),
            "project_id" => EvaluateStringCondition(invoice.ProjectId, condition),
            "supplier_id" => EvaluateNumericCondition(invoice.SupplierId, condition),
            "currency" => EvaluateStringCondition(invoice.Currency, condition),
            _ => false
        };
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rule actions for rule: {RuleId}", rule.Id);
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
        }
    }

    private async Task AutoApproveInvoiceAsync(Invoice invoice)
    {
        invoice.Status = InvoiceStatus.Freigegeben;
        invoice.AutoApproved = true;
        invoice.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Invoice auto-approved: {InvoiceId}", invoice.Id);
    }

    private async Task CreateApprovalWorkflowStepsAsync(Invoice invoice, ApprovalRule rule, RuleAction action)
    {
        var approvers = await GetApproversForActionAsync(invoice, action);
        
        int stepNumber = 1;
        foreach (var approverId in approvers)
        {
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

    private async Task CreateDefaultApprovalWorkflowAsync(Invoice invoice)
    {
        var approvers = new List<int>();

        // Add cost center manager if available
        if (invoice.CostCenter?.ManagerId.HasValue == true)
        {
            approvers.Add(invoice.CostCenter.ManagerId.Value);
        }

        // Add project manager if available and different from cost center manager
        if (invoice.Project?.ProjectManagerId.HasValue == true && 
            invoice.Project.ProjectManagerId != invoice.CostCenter?.ManagerId)
        {
            approvers.Add(invoice.Project.ProjectManagerId.Value);
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
}

// Helper classes for JSON deserialization
public class RuleCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? LogicalOperator { get; set; }
}

public class RuleAction
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}