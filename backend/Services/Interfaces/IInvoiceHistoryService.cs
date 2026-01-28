using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Text.Json;


namespace RechnungsfreigabeAPI.Services.Interfaces;

public interface IInvoiceHistoryService
{
    Task CreateHistoryEntryAsync(CreateHistoryEntryDto createHistoryDto);
    Task<List<InvoiceHistoryDto>> GetInvoiceHistoryAsync(int invoiceId);
    Task<List<InvoiceHistoryTimelineDto>> GetInvoiceHistoryTimelineAsync(int invoiceId);
    Task CreateSystemActionAsync(int invoiceId, string action, HistoryActionType actionType, string? systemReason = null, string? policyReference = null);
    Task CreateEscalationAsync(int invoiceId, string reason, int? escalatedTo = null);
    Task CreateStatusChangeAsync(int invoiceId, string oldStatus, string newStatus, int changedBy, string? comments = null);
    Task CreateDataCompletionAsync(int invoiceId, List<FieldChangeDto> fieldChanges, int changedBy);
    Task CreateApprovalActionAsync(int invoiceId, bool approved, int approverId, string? comments = null);
    Task CreateAssignmentAsync(int invoiceId, int assignedTo, string policyReference, int? assignedBy = null);
}
