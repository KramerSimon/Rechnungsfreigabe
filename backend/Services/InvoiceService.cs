using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Linq.Expressions;

namespace RechnungsfreigabeAPI.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(PageRequest pageRequest, int userId, string[] userPermissions);
    Task<PagedResult<InvoiceDto>> GetAllInvoicesPagedAsync(PageRequest pageRequest);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id, int userId, string[] userPermissions);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto, int createdBy);
    Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto, int updatedBy);
    Task<bool> DeleteInvoiceAsync(int id);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<IEnumerable<InvoiceDto>> GetPendingApprovalsAsync(int userId);
    Task<bool> ApproveInvoiceAsync(int invoiceId, int approverId, ApproveInvoiceDto approveDto);
    Task<InvoiceDto?> UpdateInvoiceStatusAsync(int id, string statusCode, int updatedBy);

    // Neue Statistik-Methoden für Dashboard
    Task<double> GetAutoApprovalRateAsync();
    Task<int> GetInvoiceCountThisMonthAsync();
    Task<double> GetAverageProcessingTimeAsync();
    Task<(int Count, decimal Amount)> GetRejectedInvoiceStatsAsync();
    Task<(int Count, decimal Amount)> GetReadyForPaymentStatsAsync();
    Task<decimal> GetOpenVolumeAmountAsync();
}

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IApprovalService _approvalService;
    private readonly INotificationService _notificationService;
    private readonly IInvoiceHistoryService _historyService;
    private readonly IUserService _userService;

    public InvoiceService(
        ApplicationDbContext context,
        IApprovalService approvalService,
        INotificationService notificationService,
        IInvoiceHistoryService historyService,
        IUserService userService)
    {
        _context = context;
        _approvalService = approvalService;
        _notificationService = notificationService;
        _historyService = historyService;
        _userService = userService;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(PageRequest pageRequest, int userId, string[] userPermissions)
    {
        var query = _context.Invoices
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .AsQueryable();

        // Apply permission-based filtering
        query = await ApplyPermissionFilterAsync(query, userId, userPermissions);

        // Apply search filter
        if (!string.IsNullOrEmpty(pageRequest.SearchTerm))
        {
            var searchTerm = pageRequest.SearchTerm.ToLower();
            query = query.Where(i => 
                i.InvoiceNumber.ToLower().Contains(searchTerm) ||
                i.Supplier.Name.ToLower().Contains(searchTerm) ||
                (i.Description != null && i.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = pageRequest.SortBy?.ToLower() switch
        {
            "invoicenumber" => pageRequest.SortDescending ? query.OrderByDescending(i => i.InvoiceNumber) : query.OrderBy(i => i.InvoiceNumber),
            "supplier" => pageRequest.SortDescending ? query.OrderByDescending(i => i.Supplier.Name) : query.OrderBy(i => i.Supplier.Name),
            "totalamount" => pageRequest.SortDescending ? query.OrderByDescending(i => i.TotalAmount) : query.OrderBy(i => i.TotalAmount),
            "invoicedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderBy(i => i.InvoiceDate),
            "duedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.DueDate) : query.OrderBy(i => i.DueDate),
            "status" => pageRequest.SortDescending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            "receivedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.ReceivedDate) : query.OrderBy(i => i.ReceivedDate),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        
        var invoices = await query
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync();

        return new PagedResult<InvoiceDto>
        {
            Items = invoices.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageRequest.PageNumber,
            PageSize = pageRequest.PageSize
        };
    }

    public async Task<PagedResult<InvoiceDto>> GetAllInvoicesPagedAsync(PageRequest pageRequest)
    {
        var query = _context.Invoices
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(pageRequest.SearchTerm))
        {
            var searchTerm = pageRequest.SearchTerm.ToLower();
            query = query.Where(i => 
                i.InvoiceNumber.ToLower().Contains(searchTerm) ||
                i.Supplier.Name.ToLower().Contains(searchTerm) ||
                (i.Description != null && i.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = pageRequest.SortBy?.ToLower() switch
        {
            "invoicenumber" => pageRequest.SortDescending ? query.OrderByDescending(i => i.InvoiceNumber) : query.OrderBy(i => i.InvoiceNumber),
            "supplier" => pageRequest.SortDescending ? query.OrderByDescending(i => i.Supplier.Name) : query.OrderBy(i => i.Supplier.Name),
            "totalamount" => pageRequest.SortDescending ? query.OrderByDescending(i => i.TotalAmount) : query.OrderBy(i => i.TotalAmount),
            "invoicedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderBy(i => i.InvoiceDate),
            "duedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.DueDate) : query.OrderBy(i => i.DueDate),
            "status" => pageRequest.SortDescending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            "receivedate" => pageRequest.SortDescending ? query.OrderByDescending(i => i.ReceivedDate) : query.OrderBy(i => i.ReceivedDate),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        
        var invoices = await query
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync();

        return new PagedResult<InvoiceDto>
        {
            Items = invoices.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageRequest.PageNumber,
            PageSize = pageRequest.PageSize
        };
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id, int userId, string[] userPermissions)
    {
        var query = _context.Invoices
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .Where(i => i.Id == id);

        // Apply permission filtering
        query = await ApplyPermissionFilterAsync(query, userId, userPermissions);
        
        var invoice = await query.FirstOrDefaultAsync();
        return invoice != null ? MapToDto(invoice) : null;
    }

    private async Task<InvoiceDto?> GetInvoiceByIdInternalAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Status)
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Status)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto, int createdBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = new Invoice
            {
                InvoiceNumber = createInvoiceDto.InvoiceNumber,
                SupplierId = createInvoiceDto.SupplierId,
                PurchaseOrderId = createInvoiceDto.PurchaseOrderId,
                CostCenterId = createInvoiceDto.CostCenterId,
                ProjectId = createInvoiceDto.ProjectId,
                NetAmount = createInvoiceDto.NetAmount,
                TaxAmount = createInvoiceDto.TaxAmount,
                TotalAmount = createInvoiceDto.TotalAmount,
                Currency = createInvoiceDto.Currency,
                InvoiceDate = createInvoiceDto.InvoiceDate,
                DueDate = createInvoiceDto.DueDate,
                Description = createInvoiceDto.Description,
                InternalNotes = createInvoiceDto.InternalNotes,
                RequiresApproval = createInvoiceDto.RequiresApproval,
                StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            
            try
            {
                Console.WriteLine($"[InvoiceService] About to call SaveChangesAsync...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"[InvoiceService] SaveChangesAsync completed successfully");
            }
            catch (DbUpdateException dbEx)
            {
                // Mehr Details bei Datenbankfehlern
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                var fullMessage = $"Database error: {dbEx.Message}";
                if (dbEx.InnerException != null)
                {
                    fullMessage += $" | Inner: {innerMessage}";
                    if (dbEx.InnerException.InnerException != null)
                    {
                        fullMessage += $" | InnerInner: {dbEx.InnerException.InnerException.Message}";
                    }
                }
                Console.WriteLine($"[ERROR] {fullMessage}");
                throw new InvalidOperationException(fullMessage, dbEx);
            }

            // Create approval workflows based on rules
            if (createInvoiceDto.RequiresApproval)
            {
                await _approvalService.CreateApprovalWorkflowAsync(invoice.Id);
            }

            // Create audit trail entry
            await _historyService.CreateHistoryEntryAsync(new CreateHistoryEntryDto
            {
                InvoiceId = invoice.Id,
                Action = "Rechnung importiert",
                ActionType = HistoryActionType.Created.ToString(),
                ActionSource = HistoryActionSource.Import.ToString(),
                NewStatus = TruncateStatus(invoice.Status?.Code ?? RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen),
                ImportChannel = "E-Mail", // Default, can be parameterized later
                ChangedBy = createdBy
            });

            await transaction.CommitAsync();

            // Send notifications
            await _notificationService.NotifyInvoiceCreatedAsync(invoice.Id);

            // Return the created invoice with full details
            return await GetInvoiceByIdInternalAsync(invoice.Id) ?? throw new InvalidOperationException("Failed to retrieve created invoice");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            
            throw;
        }
    }

    public async Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto, int updatedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return null;

            var oldStatus = invoice.Status?.ToString() ?? string.Empty;
            var changes = new List<string>();

            // Update invoice properties and track changes
            if (!string.IsNullOrEmpty(updateInvoiceDto.InvoiceNumber) && invoice.InvoiceNumber != updateInvoiceDto.InvoiceNumber)
            {
                changes.Add($"InvoiceNumber: {invoice.InvoiceNumber} -> {updateInvoiceDto.InvoiceNumber}");
                invoice.InvoiceNumber = updateInvoiceDto.InvoiceNumber;
            }

            if (updateInvoiceDto.SupplierId.HasValue && invoice.SupplierId != updateInvoiceDto.SupplierId.Value)
            {
                changes.Add($"SupplierId: {invoice.SupplierId} -> {updateInvoiceDto.SupplierId.Value}");
                invoice.SupplierId = updateInvoiceDto.SupplierId.Value;
            }

            if (updateInvoiceDto.NetAmount.HasValue && invoice.NetAmount != updateInvoiceDto.NetAmount.Value)
            {
                changes.Add($"NetAmount: {invoice.NetAmount} -> {updateInvoiceDto.NetAmount.Value}");
                invoice.NetAmount = updateInvoiceDto.NetAmount.Value;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.CostCenterId) && invoice.CostCenterId != updateInvoiceDto.CostCenterId)
            {
                changes.Add($"CostCenterId: {invoice.CostCenterId} -> {updateInvoiceDto.CostCenterId}");
                invoice.CostCenterId = updateInvoiceDto.CostCenterId;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.ProjectId) && invoice.ProjectId != updateInvoiceDto.ProjectId)
            {
                changes.Add($"ProjectId: {invoice.ProjectId} -> {updateInvoiceDto.ProjectId}");
                invoice.ProjectId = updateInvoiceDto.ProjectId;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.PurchaseOrderId) && invoice.PurchaseOrderId != updateInvoiceDto.PurchaseOrderId)
            {
                changes.Add($"PurchaseOrderId: {invoice.PurchaseOrderId} -> {updateInvoiceDto.PurchaseOrderId}");
                invoice.PurchaseOrderId = updateInvoiceDto.PurchaseOrderId;
            }

            if (updateInvoiceDto.TaxAmount.HasValue && invoice.TaxAmount != updateInvoiceDto.TaxAmount.Value)
            {
                changes.Add($"TaxAmount: {invoice.TaxAmount} -> {updateInvoiceDto.TaxAmount.Value}");
                invoice.TaxAmount = updateInvoiceDto.TaxAmount.Value;
            }

            if (updateInvoiceDto.TotalAmount.HasValue && invoice.TotalAmount != updateInvoiceDto.TotalAmount.Value)
            {
                changes.Add($"TotalAmount: {invoice.TotalAmount} -> {updateInvoiceDto.TotalAmount.Value}");
                invoice.TotalAmount = updateInvoiceDto.TotalAmount.Value;
            }

            if (updateInvoiceDto.InvoiceDate.HasValue && invoice.InvoiceDate != updateInvoiceDto.InvoiceDate.Value)
            {
                changes.Add($"InvoiceDate: {invoice.InvoiceDate} -> {updateInvoiceDto.InvoiceDate.Value}");
                invoice.InvoiceDate = updateInvoiceDto.InvoiceDate.Value;
            }

            if (updateInvoiceDto.DueDate.HasValue && invoice.DueDate != updateInvoiceDto.DueDate.Value)
            {
                changes.Add($"DueDate: {invoice.DueDate} -> {updateInvoiceDto.DueDate.Value}");
                invoice.DueDate = updateInvoiceDto.DueDate.Value;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.Description) && invoice.Description != updateInvoiceDto.Description)
            {
                changes.Add($"Description: {invoice.Description} -> {updateInvoiceDto.Description}");
                invoice.Description = updateInvoiceDto.Description;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.InternalNotes) && invoice.InternalNotes != updateInvoiceDto.InternalNotes)
            {
                changes.Add($"InternalNotes: {invoice.InternalNotes} -> {updateInvoiceDto.InternalNotes}");
                invoice.InternalNotes = updateInvoiceDto.InternalNotes;
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.Status) && invoice.Status?.Code != updateInvoiceDto.Status)
            {
                var statusId = await GetStatusIdByCodeAsync(updateInvoiceDto.Status);
                if (statusId.HasValue)
                {
                    changes.Add($"Status: {invoice.Status?.Code} -> {updateInvoiceDto.Status}");
                    invoice.StatusId = statusId;
                }
            }

            invoice.ProcessedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create audit trail entry if there were changes
            if (changes.Any())
            {
                var fieldChangesList = changes.Select(change => {
                    var parts = change.Split(':', 2);
                    var fieldName = parts[0].Trim();
                    var values = parts.Length > 1 ? parts[1].Split(" -> ") : new[] { "", "" };
                    return new FieldChangeDto
                    {
                        FieldName = fieldName,
                        DisplayName = GetFieldDisplayName(fieldName),
                        OldValue = values.Length > 0 ? values[0].Trim() : null,
                        NewValue = values.Length > 1 ? values[1].Trim() : null
                    };
                }).ToList();
                
                await _historyService.CreateDataCompletionAsync(invoice.Id, fieldChangesList, updatedBy);
            }

            await transaction.CommitAsync();

            return await GetInvoiceByIdInternalAsync(id);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.ApprovalWorkflows)
                .Include(i => i.InvoiceHistories)
                .Include(i => i.Notifications)
                .FirstOrDefaultAsync(i => i.Id == id);
            
            if (invoice == null) return false;

            // Hard delete - remove all related entities first
            if (invoice.ApprovalWorkflows != null && invoice.ApprovalWorkflows.Any())
            {
                _context.ApprovalWorkflows.RemoveRange(invoice.ApprovalWorkflows);
            }

            if (invoice.InvoiceHistories != null && invoice.InvoiceHistories.Any())
            {
                _context.InvoiceHistories.RemoveRange(invoice.InvoiceHistories);
            }

            if (invoice.Notifications != null && invoice.Notifications.Any())
            {
                _context.Notifications.RemoveRange(invoice.Notifications);
            }

            // Finally delete the invoice itself
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var statusEingegangen = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen && s.EntityType == EntityTypes.Invoice);
        var statusFreigabeErforderlich = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich && s.EntityType == EntityTypes.Invoice);
        var statusFreigegeben = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben && s.EntityType == EntityTypes.Invoice);
        var statusUeberfaellig = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Ueberfaellig && s.EntityType == EntityTypes.Invoice);
        var statusBezahlt = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt && s.EntityType == EntityTypes.Invoice);

        var stats = await _context.Invoices
            .GroupBy(i => 1)
            .Select(g => new DashboardStatsDto
            {
                NewInvoices = g.Count(i => i.StatusId == statusEingegangen!.Id),
                PendingApproval = g.Count(i => i.StatusId == statusFreigabeErforderlich!.Id),
                ApprovedInvoices = g.Count(i => i.StatusId == statusFreigegeben!.Id),
                OverdueInvoices = g.Count(i => i.StatusId == statusUeberfaellig!.Id),
                MonthlyApprovedAmount = g.Where(i => 
                    (i.StatusId == statusFreigegeben!.Id || i.StatusId == statusBezahlt!.Id) &&
                    i.InvoiceDate >= startOfMonth &&
                    i.InvoiceDate < startOfMonth.AddMonths(1))
                    .Sum(i => i.TotalAmount),
                PendingApprovalAmount = g.Where(i => i.StatusId == statusFreigabeErforderlich!.Id)
                    .Sum(i => i.TotalAmount)
            })
            .FirstOrDefaultAsync();

        return stats ?? new DashboardStatsDto();
    }

    public async Task<IEnumerable<InvoiceDto>> GetPendingApprovalsAsync(int userId)
    {
        var pendingStatus = await _context.Statuses
            .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending && 
                                        s.EntityType == EntityTypes.ApprovalWorkflow);

        var invoices = await _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Where(i => i.ApprovalWorkflows.Any(aw => aw.ApproverId == userId && aw.StatusId == pendingStatus!.Id))
            .ToListAsync();

        return invoices.Select(MapToDto);
    }

    public async Task<bool> ApproveInvoiceAsync(int invoiceId, int approverId, ApproveInvoiceDto approveDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.ApprovalWorkflows)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return false;

            // Check if user has permission to approve
            var userPermissions = await _userService.GetUserPermissionsAsync(approverId);
            var isAdmin = userPermissions.Contains("all");

            var pendingStatus2 = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending && 
                                            s.EntityType == EntityTypes.ApprovalWorkflow);
            
            var allWorkflows = await _context.ApprovalWorkflows
                .Where(aw => aw.InvoiceId == invoiceId && aw.ApproverId == approverId)
                .ToListAsync();
            
            var pendingWorkflow = allWorkflows
                .FirstOrDefault(aw => aw.StatusId == pendingStatus2?.Id);

            // If user is admin and no workflow exists for them, allow approval anyway
            if (pendingWorkflow == null && !isAdmin) return false;

            // If admin approves, mark ALL pending workflows as approved/rejected
            if (isAdmin && approveDto.Approved)
            {
                var allPendingWorkflows = invoice.ApprovalWorkflows
                    .Where(aw => aw.StatusId == pendingStatus2?.Id)
                    .ToList();

                if (allPendingWorkflows.Any())
                {
                    // Admin approves all pending workflows at once
                    foreach (var workflow in allPendingWorkflows)
                    {
                        var approvedStatus = await _context.Statuses
                            .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Approved && 
                                                        s.EntityType == EntityTypes.ApprovalWorkflow);
                        workflow.StatusId = approvedStatus?.Id;
                        workflow.Comments = $"Admin-Freigabe: {approveDto.Comments}";
                        workflow.ApprovedAt = DateTime.UtcNow;
                    }

                    invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben);
                    invoice.ProcessedBy = approverId;
                    invoice.UpdatedAt = DateTime.UtcNow;

                    await _historyService.CreateApprovalActionAsync(invoiceId, true, approverId, 
                        $"Admin hat alle ausstehenden Freigaben erteilt: {approveDto.Comments}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _notificationService.NotifyInvoiceApprovalAsync(invoiceId, true);

                    return true;
                }
                else
                {
                    // No pending workflows - admin override
                    invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben);
                    invoice.ProcessedBy = approverId;
                    invoice.UpdatedAt = DateTime.UtcNow;

                    await _historyService.CreateApprovalActionAsync(invoiceId, true, approverId, approveDto.Comments);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _notificationService.NotifyInvoiceApprovalAsync(invoiceId, true);

                    return true;
                }
            }

            // Admin rejection
            if (isAdmin && !approveDto.Approved)
            {
                var allPendingWorkflows = invoice.ApprovalWorkflows
                    .Where(aw => aw.StatusId == pendingStatus2?.Id)
                    .ToList();

                foreach (var workflow in allPendingWorkflows)
                {
                    var rejectedStatus = await _context.Statuses
                        .FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Rejected && 
                                                    s.EntityType == EntityTypes.ApprovalWorkflow);
                    workflow.StatusId = rejectedStatus?.Id;
                    workflow.Comments = $"Admin-Ablehnung: {approveDto.Comments}";
                    workflow.ApprovedAt = DateTime.UtcNow;
                }

                    invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt);
                invoice.ProcessedBy = approverId;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _historyService.CreateApprovalActionAsync(invoiceId, false, approverId, approveDto.Comments);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _notificationService.NotifyInvoiceApprovalAsync(invoiceId, false);

                return true;
            }

            // Regular user workflow
            if (pendingWorkflow == null) return false;

            // Update workflow status
            var approvalStatus = approveDto.Approved ? RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Approved : RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Rejected;
            var statusId = await _context.Statuses
                .Where(s => s.Code == approvalStatus && s.EntityType == EntityTypes.ApprovalWorkflow)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
            pendingWorkflow.StatusId = statusId;
            pendingWorkflow.Comments = approveDto.Comments;
            pendingWorkflow.ApprovedAt = DateTime.UtcNow;

            // Check if this was a rejection
            if (!approveDto.Approved)
            {
                invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt);
                invoice.ProcessedBy = approverId;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _historyService.CreateApprovalActionAsync(invoiceId, false, approverId, approveDto.Comments);
            }
            else
            {
                // Check if all required approvals are completed
                var pendingWorkflows = allWorkflows.Where(aw => aw.StatusId == pendingStatus2?.Id).ToList();

                if (pendingWorkflows.Count == 1 && pendingWorkflows.First().Id == pendingWorkflow.Id)
                {
                    // This was the last pending approval
                    invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben);
                    invoice.ProcessedBy = approverId;
                    invoice.UpdatedAt = DateTime.UtcNow;

                    await _historyService.CreateApprovalActionAsync(invoiceId, true, approverId, approveDto.Comments);
                }
                else
                {
                    var totalSteps = allWorkflows.Count;
                    var currentStep = pendingWorkflow.StepNumber;
                    await _historyService.CreateApprovalActionAsync(invoiceId, true, approverId, 
                        $"{approveDto.Comments} (Teilfreigabe - Schritt {currentStep} von {totalSteps})");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send notifications
            await _notificationService.NotifyInvoiceApprovalAsync(invoiceId, approveDto.Approved);

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            
            throw;
        }
    }

    public async Task<InvoiceDto?> UpdateInvoiceStatusAsync(int id, string statusCode, int updatedBy)
    {
        try
        {
            var invoice = await _context.Invoices.Include(i => i.Status).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return null;

            var oldStatus = invoice.Status?.Code ?? "Unknown";
            var statusId = await GetStatusIdByCodeAsync(statusCode);
            if (!statusId.HasValue)
                throw new InvalidOperationException($"Status code '{statusCode}' not found");

            invoice.StatusId = statusId;
            invoice.ProcessedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _historyService.CreateStatusChangeAsync(id, oldStatus, statusCode, updatedBy);

            return await GetInvoiceByIdInternalAsync(id);
        }
        catch (Exception)
        {
            
            throw;
        }
    }

    private async Task<IQueryable<Invoice>> ApplyPermissionFilterAsync(IQueryable<Invoice> query, int userId, string[] userPermissions)
    {
        // In Entwicklungsmodus (keine Auth) alles durchlassen, damit Detail-Views nicht leer laufen
        if (userId <= 0)
        {
            return query;
        }

        // Admin users with "all" permission can see everything
        if (userPermissions.Contains("all"))
        {
            return query;
        }

        // Users with "invoices.view_all" permission can see all invoices
        if (userPermissions.Contains("invoices.view_all"))
        {
            return query;
        }

        // Get user details for filtering
        var user = await _userService.GetUserEntityByIdAsync(userId);
        if (user == null)
        {
            // If user not found, return empty result
            return query.Where(i => false);
        }

        var filters = new List<Expression<Func<Invoice, bool>>>();

        // Users with "invoices.view_own" can see invoices they created
        if (userPermissions.Contains("invoices.view_own"))
        {
            filters.Add(i => i.CreatedBy == userId);
        }

        // Users with "invoices.view_team" can see invoices from their cost centers
        if (userPermissions.Contains("invoices.view_team"))
        {
            // Get cost centers where the user is a manager
            var managedCostCenters = await _context.CostCenters
                .Where(cc => cc.ManagerId == userId)
                .Select(cc => cc.Id)
                .ToListAsync();

            if (managedCostCenters.Any())
            {
                filters.Add(i => i.CostCenterId != null && managedCostCenters.Contains(i.CostCenterId));
            }
        }

        // Users with "invoices.approve" can see invoices they can approve
        if (userPermissions.Contains("invoices.approve"))
        {
            // Get invoices where this user is in the approval workflow
            filters.Add(i => i.ApprovalWorkflows.Any(aw => aw.ApproverId == userId));
        }

        // If no specific permissions match, deny access
        if (!filters.Any())
        {
            return query.Where(i => false);
        }

        // Combine filters with OR logic
        var combinedFilter = filters.Aggregate((filter1, filter2) => 
        {
            var parameter = Expression.Parameter(typeof(Invoice), "i");
            var body1 = Expression.Invoke(filter1, parameter);
            var body2 = Expression.Invoke(filter2, parameter);
            var combined = Expression.OrElse(body1, body2);
            return Expression.Lambda<Func<Invoice, bool>>(combined, parameter);
        });

        return query.Where(combinedFilter);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Supplier = invoice.Supplier != null ? new SupplierDto
            {
                Id = invoice.Supplier.Id,
                Name = invoice.Supplier.Name,
                Email = invoice.Supplier.Email
            } : new SupplierDto { Id = 0, Name = "N/A" },
            PurchaseOrderId = invoice.PurchaseOrderId,
            CostCenterId = invoice.CostCenterId,
            CostCenterName = invoice.CostCenter?.Name,
            ProjectId = invoice.ProjectId,
            ProjectName = invoice.Project?.Name,
            NetAmount = invoice.NetAmount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            ReceivedDate = invoice.ReceivedDate,
            Status = invoice.Status?.Code ?? RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen,
            StatusColor = invoice.Status?.Color,
            RequiresApproval = invoice.RequiresApproval,
            ApprovalLevel = invoice.ApprovalLevel,
            AutoApproved = invoice.AutoApproved,
            PdfFilePath = null,
            PdfFileSize = invoice.PdfFileSize,
            OriginalFilename = invoice.OriginalFilename,
            Description = invoice.Description,
            InternalNotes = invoice.InternalNotes,
            Creator = invoice.Creator != null ? new UserDto 
            { 
                Id = invoice.Creator.Id, 
                Username = invoice.Creator.Username,
                FirstName = invoice.Creator.FirstName,
                LastName = invoice.Creator.LastName
            } : null,
            Processor = invoice.Processor != null ? new UserDto 
            { 
                Id = invoice.Processor.Id, 
                Username = invoice.Processor.Username,
                FirstName = invoice.Processor.FirstName,
                LastName = invoice.Processor.LastName
            } : null,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            IsOverdue = invoice.DueDate < DateTime.UtcNow && 
                       invoice.Status?.Code != RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt && 
                       invoice.Status?.Code != RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Storniert,
            DaysOverdue = invoice.DueDate < DateTime.UtcNow ? (DateTime.UtcNow - invoice.DueDate).Days : 0,
            // Exponiere den gesamten Genehmigungsablauf (nicht nur offene Schritte),
            // damit das UI den vollständigen Verlauf darstellen kann.
            PendingApprovals = invoice.ApprovalWorkflows
                .OrderBy(aw => aw.StepNumber)
                .Where(aw => aw.Approver != null) // Filter out null approvers
                .Select(aw => new ApprovalWorkflowDto
                {
                    Id = aw.Id,
                    InvoiceId = aw.InvoiceId,
                    InvoiceNumber = aw.Invoice?.InvoiceNumber ?? string.Empty,
                    StepNumber = aw.StepNumber,
                    ApproverId = aw.Approver!.Id,
                    ApproverName = $"{aw.Approver!.FirstName} {aw.Approver!.LastName}".Trim(),
                    Approver = new UserDto
                    {
                        Id = aw.Approver!.Id,
                        Username = aw.Approver!.Username,
                        FirstName = aw.Approver!.FirstName,
                        LastName = aw.Approver!.LastName
                    },
                    ApprovalLevel = aw.ApprovalLevel,
                    Status = aw.Status?.Code ?? "Pending",
                    Comments = aw.Comments,
                    ApprovedAt = aw.ApprovedAt,
                    CreatedAt = aw.CreatedAt
                })
                .ToArray()
        };
    }

    // Implementierung der neuen Statistik-Methoden für Dashboard
    public async Task<double> GetAutoApprovalRateAsync()
    {
        var totalInvoices = await _context.Invoices
            .Where(i => i.CreatedAt >= DateTime.Now.AddMonths(-3)) // Letzten 3 Monate
            .CountAsync();

        if (totalInvoices == 0) return 0;

        var autoApprovedCount = await _context.Invoices
            .Where(i => i.CreatedAt >= DateTime.Now.AddMonths(-3) && i.AutoApproved)
            .CountAsync();

        return (double)autoApprovedCount / totalInvoices * 100;
    }

    public async Task<int> GetInvoiceCountThisMonthAsync()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return await _context.Invoices
            .Where(i => i.CreatedAt >= startOfMonth)
            .CountAsync();
    }

    public async Task<double> GetAverageProcessingTimeAsync()
    {
        var statusEingegangen = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen && s.EntityType == EntityTypes.Invoice);
        var statusInPruefung = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.InPruefung && s.EntityType == EntityTypes.Invoice);

        var processedInvoices = await _context.Invoices
            .Where(i => i.StatusId != statusEingegangen!.Id && 
                       i.StatusId != statusInPruefung!.Id &&
                       i.CreatedAt >= DateTime.Now.AddMonths(-3))
            .Select(i => new { 
                CreatedAt = i.CreatedAt, 
                UpdatedAt = i.UpdatedAt 
            })
            .ToListAsync();

        if (!processedInvoices.Any()) return 0;

        // Filtere nur Rechnungen, die tatsächlich verarbeitet wurden (UpdatedAt > CreatedAt)
        var validInvoices = processedInvoices
            .Where(i => i.UpdatedAt > i.CreatedAt)
            .ToList();

        if (!validInvoices.Any()) return 0;

        var avgDays = validInvoices
            .Average(i => (i.UpdatedAt - i.CreatedAt).TotalDays);

        return avgDays;
    }

    public async Task<(int Count, decimal Amount)> GetRejectedInvoiceStatsAsync()
    {
        var statusAbgelehnt = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt && s.EntityType == EntityTypes.Invoice);
        var rejectedInvoices = await _context.Invoices
            .Where(i => i.StatusId == statusAbgelehnt!.Id)
            .Select(i => i.TotalAmount)
            .ToListAsync();

        return (rejectedInvoices.Count, rejectedInvoices.Sum());
    }

    public async Task<(int Count, decimal Amount)> GetReadyForPaymentStatsAsync()
    {
        var statusFreigegeben = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben && s.EntityType == EntityTypes.Invoice);
        var readyInvoices = await _context.Invoices
            .Where(i => i.StatusId == statusFreigegeben!.Id)
            .Select(i => i.TotalAmount)
            .ToListAsync();

        return (readyInvoices.Count, readyInvoices.Sum());
    }

    public async Task<decimal> GetOpenVolumeAmountAsync()
    {
        var statusInPruefung = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.InPruefung && s.EntityType == EntityTypes.Invoice);
        var statusEingegangen = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen && s.EntityType == EntityTypes.Invoice);
        var statusFreigabeErforderlich = await _context.Statuses.FirstOrDefaultAsync(s => s.Code == RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich && s.EntityType == EntityTypes.Invoice);

        return await _context.Invoices
            .Where(i => i.StatusId == statusInPruefung!.Id || 
                       i.StatusId == statusEingegangen!.Id ||
                       i.StatusId == statusFreigabeErforderlich!.Id)
            .SumAsync(i => i.TotalAmount);
    }

    private static string GetFieldDisplayName(string fieldName)
    {
        return fieldName switch
        {
            "CostCenterId" => "Kostenstelle",
            "ProjectId" => "Projekt", 
            "SupplierId" => "Lieferant",
            "NetAmount" => "Nettobetrag",
            "TaxAmount" => "Steuerbetrag",
            "TotalAmount" => "Gesamtbetrag",
            "DueDate" => "Fälligkeitsdatum",
            "InvoiceDate" => "Rechnungsdatum",
            "Description" => "Beschreibung",
            "InternalNotes" => "Interne Notizen",
            "PurchaseOrderId" => "Bestellung",
            _ => fieldName
        };
    }

    private static string TruncateStatus(string status)
    {
        // Truncate status to 20 characters to fit database column
        return status.Length > 20 ? status.Substring(0, 20) : status;
    }

    private async Task<int?> GetStatusIdByCodeAsync(string statusCode)
    {
        var status = await _context.Statuses
            .FirstOrDefaultAsync(s => s.Code == statusCode && s.EntityType == "Invoice");
        return status?.Id;
    }
}