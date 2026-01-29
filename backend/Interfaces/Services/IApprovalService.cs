using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Text.Json;


namespace RechnungsfreigabeAPI.Interfaces.Services;

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
