using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using System.Text.Json;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class ApprovalService : IApprovalService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IInvoiceHistoryService historyService;
    private readonly INotificationService? notificationService;
    public ApprovalService(IUnitOfWork unitOfWork, IInvoiceHistoryService historyService)
    {
        this.unitOfWork = unitOfWork;
        this.historyService = historyService;
        }

    // Optional constructor overload to support notification service without breaking existing registrations
    public ApprovalService(IUnitOfWork unitOfWork, IInvoiceHistoryService historyService, INotificationService notificationService)
    {
        this.unitOfWork = unitOfWork;
        this.historyService = historyService;
        this.notificationService = notificationService;
    }

    public async Task CreateApprovalWorkflowAsync(int invoiceId)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.GetByIdWithIncludesAsync(invoiceId);

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
            var invoice = await unitOfWork.Invoices.GetWithRelationsAsync(invoiceId);

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
        return await unitOfWork.ApprovalRules.GetActiveRulesAsync();
    }

    public async Task<ApprovalRule> CreateRuleAsync(ApprovalRule rule)
    {
        try
        {
            unitOfWork.ApprovalRules.Add(rule);
            await unitOfWork.SaveChangesAsync();

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
            var rule = await unitOfWork.ApprovalRules.GetByIdAsync(ruleId);
            if (rule == null) return false;

            unitOfWork.ApprovalRules.Remove(rule);
            await unitOfWork.SaveChangesAsync();

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
            var conditions = rule.Conditions.OrderBy(c => c.ConditionOrder).ToList();

            if (!conditions.Any())
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

    private Task<bool> EvaluateConditionAsync(Invoice invoice, ApprovalRuleCondition condition)
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

    private bool EvaluateNumericCondition(decimal value, ApprovalRuleCondition condition)
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

    private bool EvaluateNumericCondition(int value, ApprovalRuleCondition condition)
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

    private bool EvaluateStringCondition(string? value, ApprovalRuleCondition condition)
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
            var actions = rule.Actions.OrderBy(a => a.ActionOrder).ToList();
            
            if (!actions.Any()) return;

            foreach (var action in actions)
            {
                await ProcessRuleActionAsync(invoice, rule, action);
            }
        }
        catch (Exception)
        {
            
        }
    }

    private async Task ProcessRuleActionAsync(Invoice invoice, ApprovalRule rule, ApprovalRuleAction action)
    {
        switch (action.ActionType.ToLower())
        {
            case "auto_approve":
                await AutoApproveInvoiceAsync(invoice);
                break;
            case "require_approval":
                await CreateApprovalWorkflowStepsAsync(invoice, rule, action);
                break;
            case "set_status":
                var newStatus = action.ActionValue != null 
                    ? await unitOfWork.Statuses.GetByCodeAndTypeAsync(action.ActionValue, EntityTypes.Invoice)
                    : null;
                if (newStatus != null)
                {
                    invoice.StatusId = newStatus.Id;
                    invoice.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.SaveChangesAsync();
                }
                break;
            case "assign_to":
                if (int.TryParse(action.ActionValue, out var assignedUserId))
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
        var freigegeben = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben,
            EntityTypes.Invoice);
        if (freigegeben != null)
        {
            invoice.StatusId = freigegeben.Id;
        }
        invoice.AutoApproved = true;
        invoice.UpdatedAt = DateTime.UtcNow;
        
        await unitOfWork.SaveChangesAsync();
        // Write history entry for automatic approval
        await historyService.CreateSystemActionAsync(
            invoice.Id,
            "Automatisch freigegeben",
            HistoryActionType.Approved,
            systemReason: "Automatische Freigabe durch Regel"
        );
    }

    private async Task CreateApprovalWorkflowStepsAsync(Invoice invoice, ApprovalRule rule, ApprovalRuleAction action)
    {
        // If explicit staged workflow is provided in rule action, honor it
        var stages = action.Stages.OrderBy(s => s.StepNumber).ToList();
        if (stages.Any())
        {
            foreach (var stage in stages)
            {
                var approverId = await ResolveStageApproverAsync(invoice, stage);
                if (!approverId.HasValue)
                {
                    Console.WriteLine($"[WARNING] No approver resolved for stage '{stage.Role?.Name ?? stage.RoleId?.ToString() ?? "n/a"}', skipping stage {stage.StepNumber}");
                    continue;
                }

                // ensure approver exists and active
                var approverExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == approverId.Value && u.IsActive) != null;
                if (!approverExists)
                {
                    Console.WriteLine($"[WARNING] Approver {approverId.Value} not found or inactive, skipping stage {stage.StepNumber}");
                    continue;
                }

                var workflow = new ApprovalWorkflow
                {
                    InvoiceId = invoice.Id,
                    RuleId = rule.Id,
                    StepNumber = stage.StepNumber,
                    ApproverId = approverId.Value,
                    ApprovalLevel = stage.ApprovalLevel,
                    StatusId = (stage.StepNumber == 1) ? 
                        (await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                            RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                            EntityTypes.ApprovalWorkflow))?.Id :
                        (await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                            RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting,
                            EntityTypes.ApprovalWorkflow))?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                unitOfWork.ApprovalWorkflows.Add(workflow);
            }
        }
        else
        {
            // Fallback to value-based resolution
            var approvers = await GetApproversForActionAsync(invoice, action);

            if (!approvers.Any())
            {
                // Keine Approver gefunden - Auto-Approve
                Console.WriteLine($"[WARNING] No approvers found for invoice {invoice.Id}, auto-approving");
                var freigegeben = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                    RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben,
                    EntityTypes.Invoice);
                if (freigegeben != null)
                {
                    invoice.StatusId = freigegeben.Id;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
                invoice.AutoApproved = true;
                await unitOfWork.SaveChangesAsync();
                await historyService.CreateSystemActionAsync(
                    invoice.Id,
                    "Automatisch freigegeben",
                    HistoryActionType.Approved,
                    systemReason: "Keine Approver gefunden (Regel)"
                );
                return;
            }

            int stepNumber = 1;
            foreach (var approverId in approvers)
            {
                // Validiere dass der Approver existiert
                var approverExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == approverId && u.IsActive) != null;
                if (!approverExists)
                {
                    Console.WriteLine($"[WARNING] Approver {approverId} not found or inactive, skipping");
                    continue;
                }

                var statusCode = stepNumber == 1 ? RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending : RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting;
                var workflowStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(statusCode, EntityTypes.ApprovalWorkflow);
                
                var workflow = new ApprovalWorkflow
                {
                    InvoiceId = invoice.Id,
                    RuleId = rule.Id,
                    StepNumber = stepNumber,
                    ApproverId = approverId,
                    ApprovalLevel = GetApprovalLevelForAction(action),
                    StatusId = workflowStatus?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                unitOfWork.ApprovalWorkflows.Add(workflow);
                stepNumber++;
            }
        }

        var freigabeErforderlich = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich,
            EntityTypes.Invoice);
        if (freigabeErforderlich != null)
        {
            invoice.StatusId = freigabeErforderlich.Id;
        }
        await unitOfWork.SaveChangesAsync();
    }

    private async Task AssignInvoiceToUserAsync(Invoice invoice, ApprovalRule rule, int userId)
    {
        var userExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive) != null;
        if (!userExists)
        {
            
            return;
        }

        var pendingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
            EntityTypes.ApprovalWorkflow);
        
        var workflow = new ApprovalWorkflow
        {
            InvoiceId = invoice.Id,
            RuleId = rule.Id,
            StepNumber = 1,
            ApproverId = userId,
            ApprovalLevel = 1,
            StatusId = pendingStatus?.Id,
            CreatedAt = DateTime.UtcNow
        };

        unitOfWork.ApprovalWorkflows.Add(workflow);

        var freigabeErforderlich = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich,
            EntityTypes.Invoice);
        if (freigabeErforderlich != null)
        {
            invoice.StatusId = freigabeErforderlich.Id;
            invoice.UpdatedAt = DateTime.UtcNow;
        }
        await unitOfWork.SaveChangesAsync();
    }

    private async Task CreateDefaultApprovalWorkflowAsync(Invoice invoice)
    {
        var approvers = new List<int>();

        // Add cost center manager if available
        if (invoice.CostCenter?.ManagerId.HasValue == true)
        {
            var managerExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == invoice.CostCenter.ManagerId.Value && u.IsActive) != null;
            if (managerExists)
            {
                approvers.Add(invoice.CostCenter.ManagerId.Value);
            }
        }

        // Add project manager if available and different from cost center manager
        if (invoice.Project?.ProjectManagerId.HasValue == true && 
            invoice.Project.ProjectManagerId != invoice.CostCenter?.ManagerId)
        {
            var pmExists = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == invoice.Project.ProjectManagerId.Value && u.IsActive) != null;
            if (pmExists)
            {
                approvers.Add(invoice.Project.ProjectManagerId.Value);
            }
        }

        // If no specific approvers, find users with approval permissions
        if (!approvers.Any())
        {
            var allUsers = await unitOfWork.Users.GetAllWithRolesAsync();
            var approverUsers = allUsers
                .Where(u => u.IsActive && u.UserRoles.Any(ur => 
                    ur.Role.Name == "Freigeber" || ur.Role.Name == "Manager"))
                .Take(1)
                .Select(u => u.Id)
                .ToList();

            approvers.AddRange(approverUsers);
        }

        int stepNumber = 1;
        foreach (var approverId in approvers)
        {
            var statusCode = stepNumber == 1 ? RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending : RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting;
            var workflowStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(statusCode, EntityTypes.ApprovalWorkflow);
            
            var workflow = new ApprovalWorkflow
            {
                InvoiceId = invoice.Id,
                StepNumber = stepNumber++,
                ApproverId = approverId,
                ApprovalLevel = 1,
                StatusId = workflowStatus?.Id,
                CreatedAt = DateTime.UtcNow
            };

            unitOfWork.ApprovalWorkflows.Add(workflow);
        }

        if (approvers.Any())
        {
            var freigabeErforderlich = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich,
                EntityTypes.Invoice);
            if (freigabeErforderlich != null)
            {
                invoice.StatusId = freigabeErforderlich.Id;
            }
        }
        else
        {
            // No approvers found, auto-approve
            Console.WriteLine($"[WARNING] No approvers found for invoice {invoice.Id}, auto-approving");
            var freigegeben2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben,
                EntityTypes.Invoice);
            if (freigegeben2 != null)
            {
                invoice.StatusId = freigegeben2.Id;
                invoice.UpdatedAt = DateTime.UtcNow;
            }
            invoice.AutoApproved = true;
            await historyService.CreateSystemActionAsync(
                invoice.Id,
                "Automatisch freigegeben",
                HistoryActionType.Approved,
                systemReason: "Keine Approver gefunden (Standard-Workflow)"
            );
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task<int[]> GetApproversForActionAsync(Invoice invoice, ApprovalRuleAction action)
    {
        var approvers = new List<int>();

        var actionValue = action.ActionValue?.ToLower() ?? "";
        switch (actionValue)
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
                var allAdminUsers = await unitOfWork.Users.GetAllWithRolesAsync();
                var adminUser = allAdminUsers
                    .FirstOrDefault(u => u.IsActive && 
                        u.UserRoles.Any(ur => ur.Role.Name == "Administrator"));
                if (adminUser != null)
                    approvers.Add(adminUser.Id);
                break;
            default:
                // Support comma-separated roles (e.g., "manager,administrator")
                var parts = actionValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (part.Equals("manager", StringComparison.OrdinalIgnoreCase))
                    {
                        if (invoice.CostCenter?.ManagerId.HasValue == true)
                            approvers.Add(invoice.CostCenter.ManagerId.Value);
                    }
                    else if (part.Equals("project_manager", StringComparison.OrdinalIgnoreCase))
                    {
                        if (invoice.Project?.ProjectManagerId.HasValue == true)
                            approvers.Add(invoice.Project.ProjectManagerId.Value);
                    }
                    else if (part.Equals("administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        var allAdminUsers2 = await unitOfWork.Users.GetAllWithRolesAsync();
                        var admin = allAdminUsers2.FirstOrDefault(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Administrator"));
                        if (admin != null) approvers.Add(admin.Id);
                    }
                }
                // Standard approval - cost center manager if none resolved
                if (!approvers.Any() && invoice.CostCenter?.ManagerId.HasValue == true)
                    approvers.Add(invoice.CostCenter.ManagerId.Value);
                break;
        }

        return approvers.ToArray();
    }

    private async Task<int?> ResolveStageApproverAsync(Invoice invoice, ApprovalRuleStage stage)
    {
        if (stage.UserId.HasValue)
            return stage.UserId.Value;

        if (stage.RoleId.HasValue)
        {
            var users = await unitOfWork.Users.GetByRoleAsync(stage.RoleId.Value);
            var activeUser = users.FirstOrDefault(u => u.IsActive);
            if (activeUser != null)
                return activeUser.Id;
        }

        var role = stage.Role?.Name?.ToLower();
        switch (role)
        {
            case "manager":
            case "cost_center_manager":
                return invoice.CostCenter?.ManagerId;
            case "project_manager":
                return invoice.Project?.ProjectManagerId;
            case "administrator":
            case "admin":
                var allAdminUsers3 = await unitOfWork.Users.GetAllWithRolesAsync();
                var admin = allAdminUsers3.FirstOrDefault(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Administrator"));
                return admin?.Id;
            default:
                // Fallback: cost center manager
                return invoice.CostCenter?.ManagerId;
        }
    }

    private int GetApprovalLevelForAction(ApprovalRuleAction action)
    {
        return (action.ActionValue?.ToLower()) switch
        {
            "double" => 2,
            _ => 1
        };
    }

    public async Task<IEnumerable<ApprovalWorkflowDto>> GetPendingApprovalsAsync(int userId)
    {
        try
        {
            var pendingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                EntityTypes.ApprovalWorkflow);

            var allWorkflows = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var workflows = allWorkflows
                .Where(w => w.ApproverId == userId && w.StatusId == pendingStatus!.Id)
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
                    Status = w.Status != null ? w.Status.Code : "Unknown",
                    StatusColor = w.Status != null ? w.Status.Color : null,
                    Comments = w.Comments,
                    ApprovedAt = w.ApprovedAt,
                    CreatedAt = w.CreatedAt
                });

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
            var allApprovals = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var approval = allApprovals.FirstOrDefault(w => w.Id == approvalId);

            var pendingStatus3 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                EntityTypes.ApprovalWorkflow);
            
            if (approval == null || approval.StatusId != pendingStatus3?.Id)
                return false;

            if (approval.ApproverId != userId)
            {
                
                return false;
            }

            // Block approval if required data is missing
            if (string.IsNullOrWhiteSpace(approval.Invoice.CostCenterId) || string.IsNullOrWhiteSpace(approval.Invoice.ProjectId))
            {
                return false;
            }

            var approvedStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Approved,
                EntityTypes.ApprovalWorkflow);
            approval.StatusId = approvedStatus?.Id;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Comments = comments;
            var invoice = approval.Invoice;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Activate the next waiting step (sequential progression)
            var waitingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting,
                EntityTypes.ApprovalWorkflow);
            var allWorkflows2 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var nextStep = allWorkflows2
                .Where(w => w.InvoiceId == invoice.Id && w.StatusId == waitingStatus!.Id && w.StepNumber > approval.StepNumber)
                .OrderBy(w => w.StepNumber)
                .FirstOrDefault();

            if (nextStep != null)
            {
                var pendingStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                    RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                    EntityTypes.ApprovalWorkflow);
                nextStep.StatusId = pendingStatus2?.Id;
                invoice.UpdatedAt = DateTime.UtcNow;
                // Notify next approver if notification service is available
                if (notificationService != null)
                {
                    try { await notificationService.EnsureApprovalNotificationForApproverAsync(nextStep.InvoiceId, nextStep.ApproverId); } catch { }
                }
            }

            // If no pending or waiting approvals remain, finalize invoice approval
            var pendingStatusObj = await unitOfWork.Statuses.GetByCodeAndTypeAsync("Pending", "ApprovalWorkflow");
            var pendingStatusId = pendingStatusObj?.Id ?? 0;
            var waitingStatusObj = await unitOfWork.Statuses.GetByCodeAndTypeAsync("Waiting", "ApprovalWorkflow");
            var waitingStatusId = waitingStatusObj?.Id ?? 0;
            
            var allWorkflows3 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var remainingOpen = allWorkflows3.Count(w => w.InvoiceId == invoice.Id && (w.StatusId == pendingStatusId || w.StatusId == waitingStatusId));

            if (remainingOpen == 0)
            {
                var freigegeben = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                    RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben,
                    "Invoice");
                invoice.StatusId = freigegeben?.Id;
            }

            await unitOfWork.SaveChangesAsync();
            
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
            var pendingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                EntityTypes.ApprovalWorkflow);

            var allApprovals2 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var approval = allApprovals2.FirstOrDefault(w => w.Id == approvalId);

            if (approval == null || approval.StatusId != pendingStatus?.Id)
                return false;

            if (approval.ApproverId != userId)
            {
                
                return false;
            }

            var rejectedStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Rejected,
                EntityTypes.ApprovalWorkflow);
            approval.StatusId = rejectedStatus?.Id;
            approval.ApprovedAt = DateTime.UtcNow;
            approval.Comments = comments;

            // Mark invoice as rejected
            var invoice = approval.Invoice;
            var abgelehnt = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt,
                EntityTypes.Invoice);
            if (abgelehnt != null)
            {
                invoice.StatusId = abgelehnt.Id;
            }

            // Reject all open approvals (pending or waiting) for this invoice
            var pendingStatus3 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                EntityTypes.ApprovalWorkflow);
            var waitingStatus3 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting,
                EntityTypes.ApprovalWorkflow);
            
            var allWorkflows4 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var allApprovals = allWorkflows4
                .Where(w => w.InvoiceId == invoice.Id && (w.StatusId == pendingStatus3!.Id || w.StatusId == waitingStatus3!.Id))
                .ToList();

            var rejectedStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Rejected,
                EntityTypes.ApprovalWorkflow);
            
            foreach (var open in allApprovals)
            {
                open.StatusId = rejectedStatus2?.Id;
            }

            await unitOfWork.SaveChangesAsync();
            
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
            var allApprovals5 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var approval = allApprovals5.FirstOrDefault(w => w.Id == approvalId);

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
            var allWorkflows6 = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var workflows = allWorkflows6
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new ApprovalWorkflowDto
                {
                    Id = w.Id,
                    InvoiceId = w.InvoiceId,
                    StepNumber = w.StepNumber,
                    ApproverName = w.Approver != null ? $"{w.Approver.FirstName} {w.Approver.LastName}" : "Unknown",
                    Status = w.Status != null ? w.Status.Code : "Pending",
                    StatusColor = w.Status != null ? w.Status.Color : null,
                    Comments = w.Comments,
                    ApprovedAt = w.ApprovedAt,
                    CreatedAt = w.CreatedAt,
                    ApproverId = w.ApproverId,
                    ApprovalLevel = w.ApprovalLevel
                })
                .ToList();

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
            var workflow = await unitOfWork.ApprovalWorkflows.GetByIdAsync(workflowId);
            if (workflow == null) return false;

            unitOfWork.ApprovalWorkflows.Remove(workflow);
            await unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            return false;
        }
    }
}