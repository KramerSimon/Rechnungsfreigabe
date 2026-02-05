using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Linq.Expressions;
using RechnungsfreigabeAPI.Interfaces.Services;

namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IApprovalService approvalService;
    private readonly INotificationService notificationService;
    private readonly IInvoiceHistoryService historyService;
    private readonly IUserService userService;

    public InvoiceService(
        IUnitOfWork unitOfWork,
        IApprovalService approvalService,
        INotificationService notificationService,
        IInvoiceHistoryService historyService,
        IUserService userService)
    {
        this.unitOfWork = unitOfWork;
        this.approvalService = approvalService;
        this.notificationService = notificationService;
        this.historyService = historyService;
        this.userService = userService;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(PageRequest pageRequest, int userId, string[] userPermissions)
    {
        var query = unitOfWork.Invoices.GetAllWithFullDetailsQuery();

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
        var query = unitOfWork.Invoices.GetAllWithFullDetailsQuery();

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
        var query = unitOfWork.Invoices.GetAllWithFullDetailsQuery()
            .Where(i => i.Id == id);

        // Apply permission filtering
        query = await ApplyPermissionFilterAsync(query, userId, userPermissions);
        
        var invoice = await query.FirstOrDefaultAsync();
        return invoice != null ? MapToDto(invoice) : null;
    }

    private async Task<InvoiceDto?> GetInvoiceByIdInternalAsync(int id)
    {
        var invoice = await unitOfWork.Invoices.GetByIdWithFullDetailsAsync(id);
        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto, int createdBy)
    {
        await unitOfWork.BeginTransactionAsync();
        
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

            unitOfWork.Invoices.Add(invoice);
            
            try
            {
                Console.WriteLine($"[InvoiceService] About to call SaveChangesAsync...");
                await unitOfWork.SaveChangesAsync();
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
                await approvalService.CreateApprovalWorkflowAsync(invoice.Id);
            }

            // Create audit trail entry
            await historyService.CreateHistoryEntryAsync(new CreateHistoryEntryDto
            {
                InvoiceId = invoice.Id,
                Action = "Rechnung importiert",
                ActionType = HistoryActionType.Created.ToString(),
                ActionSource = HistoryActionSource.Import.ToString(),
                NewStatus = TruncateStatus(invoice.Status?.Code ?? RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen),
                ImportChannel = "E-Mail", // Default, can be parameterized later
                ChangedBy = createdBy
            });

            await unitOfWork.CommitTransactionAsync();

            // Send notifications
            await notificationService.NotifyInvoiceCreatedAsync(invoice.Id);

            // Return the created invoice with full details
            return await GetInvoiceByIdInternalAsync(invoice.Id) ?? throw new InvalidOperationException("Failed to retrieve created invoice");
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            
            throw;
        }
    }

    public async Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto, int updatedBy)
    {
        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            var invoice = await unitOfWork.Invoices.GetByIdAsync(id);
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

            await unitOfWork.SaveChangesAsync();

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
                
                await historyService.CreateDataCompletionAsync(invoice.Id, fieldChangesList, updatedBy);
            }

            await unitOfWork.CommitTransactionAsync();

            return await GetInvoiceByIdInternalAsync(id);
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();

            // Get all workflows first
            var allWorkflows = await unitOfWork.ApprovalWorkflows.GetAllAsync();
            var invoiceWorkflows = allWorkflows.Where(w => w.InvoiceId == id).ToList();
            
            // Get all histories
            var allHistories = await unitOfWork.InvoiceHistories.GetAllAsync();
            var invoiceHistories = allHistories.Where(h => h.InvoiceId == id).ToList();
            
            // Get all notifications
            var allNotifications = await unitOfWork.Notifications.GetAllAsync();
            var invoiceNotifications = allNotifications.Where(n => n.InvoiceId == id).ToList();

            // Delete approval workflows
            foreach (var workflow in invoiceWorkflows)
            {
                unitOfWork.ApprovalWorkflows.Remove(workflow);
            }
            
            // Delete invoice histories
            foreach (var history in invoiceHistories)
            {
                unitOfWork.InvoiceHistories.Remove(history);
            }
            
            // Delete notifications
            foreach (var notification in invoiceNotifications)
            {
                unitOfWork.Notifications.Remove(notification);
            }

            // Save changes for related entities
            if (invoiceWorkflows.Any() || invoiceHistories.Any() || invoiceNotifications.Any())
            {
                await unitOfWork.SaveChangesAsync();
            }

            // Get and delete the invoice itself
            var invoice = await unitOfWork.Invoices.GetByIdAsync(id);
            if (invoice == null)
            {
                await unitOfWork.RollbackTransactionAsync();
                return false;
            }

            unitOfWork.Invoices.Remove(invoice);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch (Exception ex)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch { }
            
            Console.WriteLine($"Error deleting invoice {id}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var statusEingegangen = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen, EntityTypes.Invoice);
        var statusFreigabeErforderlich = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.FreigabeErforderlich, EntityTypes.Invoice);
        var statusFreigegeben = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben, EntityTypes.Invoice);
        var statusUeberfaellig = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Ueberfaellig, EntityTypes.Invoice);
        var statusBezahlt = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt, EntityTypes.Invoice);

        var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
        
        var stats = new DashboardStatsDto
        {
            NewInvoices = allInvoices.Count(i => i.StatusId == statusEingegangen!.Id),
            PendingApproval = allInvoices.Count(i => i.StatusId == statusFreigabeErforderlich!.Id),
            ApprovedInvoices = allInvoices.Count(i => i.StatusId == statusFreigegeben!.Id),
            OverdueInvoices = allInvoices.Count(i => i.StatusId == statusUeberfaellig!.Id),
            MonthlyApprovedAmount = allInvoices
                .Where(i => 
                    (i.StatusId == statusFreigegeben!.Id || i.StatusId == statusBezahlt!.Id) &&
                    i.InvoiceDate >= startOfMonth &&
                    i.InvoiceDate < startOfMonth.AddMonths(1))
                .Sum(i => i.TotalAmount),
            PendingApprovalAmount = allInvoices.Where(i => i.StatusId == statusFreigabeErforderlich!.Id)
                .Sum(i => i.TotalAmount)
        };

        return stats;
    }

    public async Task<IEnumerable<InvoiceDto>> GetPendingApprovalsAsync(int userId)
    {
        var userPermissions = await userService.GetUserPermissionsAsync(userId);
        var canApprove = userPermissions.Contains("invoices.approve") ||
                         userPermissions.Contains("invoices.approve_cost_center");

        if (!canApprove)
        {
            return Enumerable.Empty<InvoiceDto>();
        }

        var pendingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
            EntityTypes.ApprovalWorkflow);
        var waitingStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting,
            EntityTypes.ApprovalWorkflow);

        // Use the full details query which includes ApprovalWorkflows
        var allInvoices = await unitOfWork.Invoices.GetAllWithFullDetailsQuery().ToListAsync();
        
        // Include both pending approvals AND waiting approvals for second-level+ approvers
        var invoices = allInvoices
            .Where(i => i.ApprovalWorkflows != null && i.ApprovalWorkflows.Any(aw => 
                aw.ApproverId == userId && (aw.StatusId == pendingStatus!.Id || 
                (aw.StatusId == waitingStatus!.Id && aw.StepNumber > 1))))
            .ToList();

        return invoices.Select(MapToDto);
    }

    public async Task<bool> ApproveInvoiceAsync(int invoiceId, int approverId, ApproveInvoiceDto approveDto)
    {
        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            var invoice = await unitOfWork.Invoices.GetByIdWithFullDetailsAsync(invoiceId);

            if (invoice == null) return false;

            // Check if user has permission to approve
            var userPermissions = await userService.GetUserPermissionsAsync(approverId);
            var canApprove = userPermissions.Contains("invoices.approve") ||
                             userPermissions.Contains("invoices.approve_cost_center");

            if (!canApprove)
            {
                return false;
            }

            var pendingStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Pending,
                EntityTypes.ApprovalWorkflow);
            var waitingStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Waiting,
                EntityTypes.ApprovalWorkflow);
            var approvedStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Approved,
                EntityTypes.ApprovalWorkflow);
            var rejectedStatus2 = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
                RechnungsfreigabeAPI.Models.StatusCodes.ApprovalWorkflow.Rejected,
                EntityTypes.ApprovalWorkflow);
            
            var allWorkflows = (await unitOfWork.ApprovalWorkflows.GetAllAsync())
                .Where(aw => aw.InvoiceId == invoiceId && aw.ApproverId == approverId)
                .ToList();
            
            var pendingWorkflow = allWorkflows
                .FirstOrDefault(aw => aw.StatusId == pendingStatus2?.Id);

            if (pendingWorkflow == null) return false;

            // Update workflow status
            pendingWorkflow.StatusId = approveDto.Approved ? approvedStatus2?.Id : rejectedStatus2?.Id;
            pendingWorkflow.Comments = approveDto.Comments;
            pendingWorkflow.ApprovedAt = DateTime.UtcNow;

            // Check if this was a rejection
            if (!approveDto.Approved)
            {
                invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt);
                invoice.ProcessedBy = approverId;
                invoice.UpdatedAt = DateTime.UtcNow;

                await historyService.CreateApprovalActionAsync(invoiceId, false, approverId, approveDto.Comments);
            }
            else
            {
                var allWorkflowsForInvoice = invoice.ApprovalWorkflows.ToList();
                var pendingWorkflows = allWorkflowsForInvoice.Where(aw => aw.StatusId == pendingStatus2?.Id).ToList();

                if (!pendingWorkflows.Any())
                {
                    var nextWaiting = allWorkflowsForInvoice
                        .Where(aw => aw.StatusId == waitingStatus2?.Id && aw.StepNumber > pendingWorkflow.StepNumber)
                        .OrderBy(aw => aw.StepNumber)
                        .FirstOrDefault();

                    if (nextWaiting != null)
                    {
                        nextWaiting.StatusId = pendingStatus2?.Id;
                        await historyService.CreateApprovalActionAsync(invoiceId, true, approverId,
                            approveDto.Comments);
                    }
                    else
                    {
                        invoice.StatusId = await GetStatusIdByCodeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben);
                        invoice.ProcessedBy = approverId;
                        invoice.UpdatedAt = DateTime.UtcNow;

                        await historyService.CreateApprovalActionAsync(invoiceId, true, approverId, approveDto.Comments);
                    }
                }
                else
                {
                    var totalSteps = allWorkflowsForInvoice.Count;
                    var currentStep = pendingWorkflow.StepNumber;
                    await historyService.CreateApprovalActionAsync(invoiceId, true, approverId,
                        $"{approveDto.Comments} (Teilfreigabe - Schritt {currentStep} von {totalSteps})");
                }
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            // Send notifications
            await notificationService.NotifyInvoiceApprovalAsync(invoiceId, approveDto.Approved);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackTransactionAsync();
            
            throw;
        }
    }

    public async Task<InvoiceDto?> UpdateInvoiceStatusAsync(int id, string statusCode, int updatedBy)
    {
        try
        {
            var invoice = await unitOfWork.Invoices.GetByIdWithFullDetailsAsync(id);
            if (invoice == null) return null;

            var oldStatus = invoice.Status?.Code ?? "Unknown";
            var statusId = await GetStatusIdByCodeAsync(statusCode);
            if (!statusId.HasValue)
                throw new InvalidOperationException($"Status code '{statusCode}' not found");

            invoice.StatusId = statusId;
            invoice.ProcessedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();

            await historyService.CreateStatusChangeAsync(id, oldStatus, statusCode, updatedBy);

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

        // Users with "invoices.view_all" permission can see all invoices
        if (userPermissions.Contains("invoices.view_all"))
        {
            return query;
        }

        // Get user details for filtering
        var user = await userService.GetUserEntityByIdAsync(userId);
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
            var allCostCenters = await unitOfWork.CostCenters.GetAllAsync();
            var managedCostCenters = allCostCenters
                .Where(cc => cc.ManagerId == userId)
                .Select(cc => cc.Id)
                .ToList();

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
            // damit das UI den vollst�ndigen Verlauf darstellen kann.
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
                    StatusColor = aw.Status?.Color,
                    Comments = aw.Comments,
                    ApprovedAt = aw.ApprovedAt,
                    CreatedAt = aw.CreatedAt
                })
                .ToArray()
        };
    }

    // Implementierung der neuen Statistik-Methoden f�r Dashboard
    public async Task<double> GetAutoApprovalRateAsync()
    {
        var allInvoicesRecent = (await unitOfWork.Invoices.GetAllAsync())
            .Where(i => i.CreatedAt >= DateTime.Now.AddMonths(-3))
            .ToList();
            
        var totalInvoices = allInvoicesRecent.Count;

        if (totalInvoices == 0) return 0;

        var autoApprovedCount = allInvoicesRecent.Count(i => i.AutoApproved);

        return (double)autoApprovedCount / totalInvoices * 100;
    }

    public async Task<int> GetInvoiceCountThisMonthAsync()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var allInvoices = await unitOfWork.Invoices.GetAllAsync();
        return allInvoices.Count(i => i.CreatedAt >= startOfMonth);
    }

    public async Task<double> GetAverageProcessingTimeAsync()
    {
        var statusEingegangen = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Eingegangen, EntityTypes.Invoice);
        var statusInPruefung = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.InPruefung, EntityTypes.Invoice);

        var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
        var processedInvoices = allInvoices
            .Where(i => i.StatusId != statusEingegangen!.Id && 
                       i.StatusId != statusInPruefung!.Id &&
                       i.CreatedAt >= DateTime.Now.AddMonths(-3))
            .Select(i => new { 
                CreatedAt = i.CreatedAt, 
                UpdatedAt = i.UpdatedAt 
            })
            .ToList();

        if (!processedInvoices.Any()) return 0;

        // Filtere nur Rechnungen, die tats�chlich verarbeitet wurden (UpdatedAt > CreatedAt)
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
        var statusAbgelehnt = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt, EntityTypes.Invoice);
        var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
        var rejectedInvoices = allInvoices
            .Where(i => i.StatusId == statusAbgelehnt!.Id)
            .Select(i => i.TotalAmount)
            .ToList();

        return (rejectedInvoices.Count, rejectedInvoices.Sum());
    }

    public async Task<(int Count, decimal Amount)> GetReadyForPaymentStatsAsync()
    {
        var statusFreigegeben = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Freigegeben, EntityTypes.Invoice);
        var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
        var readyInvoices = allInvoices
            .Where(i => i.StatusId == statusFreigegeben!.Id)
            .Select(i => i.TotalAmount)
            .ToList();

        return (readyInvoices.Count, readyInvoices.Sum());
    }

    public async Task<decimal> GetOpenVolumeAmountAsync()
    {
        var statusBezahlt = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Bezahlt, EntityTypes.Invoice);
        var statusAbgelehnt = await unitOfWork.Statuses.GetByCodeAndTypeAsync(RechnungsfreigabeAPI.Models.StatusCodes.Invoice.Abgelehnt, EntityTypes.Invoice);

        var allInvoices = (await unitOfWork.Invoices.GetAllAsync()).ToList();
        return allInvoices
            .Where(i => i.StatusId != statusBezahlt!.Id && i.StatusId != statusAbgelehnt!.Id)
            .Sum(i => i.TotalAmount);
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
            "DueDate" => "F�lligkeitsdatum",
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
        var status = await unitOfWork.Statuses.GetByCodeAndTypeAsync(statusCode, "Invoice");
        return status?.Id;
    }
}