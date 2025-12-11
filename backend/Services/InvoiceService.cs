using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(PageRequest pageRequest, int? userId = null);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto, int createdBy);
    Task<InvoiceDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto, int updatedBy);
    Task<bool> DeleteInvoiceAsync(int id);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<IEnumerable<InvoiceDto>> GetPendingApprovalsAsync(int userId);
    Task<bool> ApproveInvoiceAsync(int invoiceId, int approverId, ApproveInvoiceDto approveDto);
    Task<InvoiceDto?> UpdateInvoiceStatusAsync(int id, InvoiceStatus status, int updatedBy);
}

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IApprovalService _approvalService;
    private readonly INotificationService _notificationService;

    public InvoiceService(
        ApplicationDbContext context, 
        ILogger<InvoiceService> logger,
        IApprovalService approvalService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _approvalService = approvalService;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(PageRequest pageRequest, int? userId = null)
    {
        var query = _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .AsQueryable();

        // Filter by user permissions if specified
        if (userId.HasValue)
        {
            // TODO: Add permission-based filtering based on user roles
            // For now, show all invoices - implement proper filtering based on user permissions
        }

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

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.Processor)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
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
                Status = InvoiceStatus.Eingegangen,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Create approval workflows based on rules
            if (createInvoiceDto.RequiresApproval)
            {
                await _approvalService.CreateApprovalWorkflowAsync(invoice.Id);
            }

            // Create audit trail entry
            await CreateInvoiceHistoryAsync(invoice.Id, "Created", null, invoice.Status.ToString(), null, createdBy);

            await transaction.CommitAsync();

            // Send notifications
            await _notificationService.NotifyInvoiceCreatedAsync(invoice.Id);

            _logger.LogInformation("Invoice created successfully: {InvoiceNumber}", invoice.InvoiceNumber);

            // Return the created invoice with full details
            return await GetInvoiceByIdAsync(invoice.Id) ?? throw new InvalidOperationException("Failed to retrieve created invoice");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating invoice: {InvoiceNumber}", createInvoiceDto.InvoiceNumber);
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

            var oldStatus = invoice.Status.ToString();
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

            if (!string.IsNullOrEmpty(updateInvoiceDto.Status) && invoice.Status.ToString() != updateInvoiceDto.Status)
            {
                if (Enum.TryParse<InvoiceStatus>(updateInvoiceDto.Status, out var newStatus))
                {
                    changes.Add($"Status: {invoice.Status} -> {newStatus}");
                    invoice.Status = newStatus;
                }
            }

            invoice.ProcessedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create audit trail entry if there were changes
            if (changes.Any())
            {
                var fieldChanges = string.Join(", ", changes);
                await CreateInvoiceHistoryAsync(id, "Updated", oldStatus, invoice.Status.ToString(), fieldChanges, updatedBy);
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Invoice updated successfully: {InvoiceId}", id);
            return await GetInvoiceByIdAsync(id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating invoice with ID: {InvoiceId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return false;

            // Soft delete - change status to cancelled
            invoice.Status = InvoiceStatus.Storniert;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice soft deleted: {InvoiceId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId}", id);
            throw;
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var stats = await _context.Invoices
            .GroupBy(i => 1)
            .Select(g => new DashboardStatsDto
            {
                NewInvoices = g.Count(i => i.Status == InvoiceStatus.Eingegangen),
                PendingApproval = g.Count(i => i.Status == InvoiceStatus.Freigabe_Erforderlich),
                ApprovedInvoices = g.Count(i => i.Status == InvoiceStatus.Freigegeben),
                OverdueInvoices = g.Count(i => i.Status == InvoiceStatus.Ueberfaellig),
                MonthlyApprovedAmount = g.Where(i => 
                    (i.Status == InvoiceStatus.Freigegeben || i.Status == InvoiceStatus.Bezahlt) &&
                    i.InvoiceDate >= startOfMonth &&
                    i.InvoiceDate < startOfMonth.AddMonths(1))
                    .Sum(i => i.TotalAmount),
                PendingApprovalAmount = g.Where(i => i.Status == InvoiceStatus.Freigabe_Erforderlich)
                    .Sum(i => i.TotalAmount)
            })
            .FirstOrDefaultAsync();

        return stats ?? new DashboardStatsDto();
    }

    public async Task<IEnumerable<InvoiceDto>> GetPendingApprovalsAsync(int userId)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.CostCenter)
            .Include(i => i.Project)
            .Include(i => i.Creator)
            .Include(i => i.ApprovalWorkflows)
            .ThenInclude(aw => aw.Approver)
            .Where(i => i.ApprovalWorkflows.Any(aw => aw.ApproverId == userId && aw.Status == ApprovalStatus.Pending))
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

            var pendingWorkflow = invoice.ApprovalWorkflows
                .FirstOrDefault(aw => aw.ApproverId == approverId && aw.Status == ApprovalStatus.Pending);

            if (pendingWorkflow == null) return false;

            // Update workflow status
            pendingWorkflow.Status = approveDto.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            pendingWorkflow.Comments = approveDto.Comments;
            pendingWorkflow.ApprovedAt = DateTime.UtcNow;

            // Check if this was a rejection
            if (!approveDto.Approved)
            {
                invoice.Status = InvoiceStatus.Abgelehnt;
                invoice.ProcessedBy = approverId;
                invoice.UpdatedAt = DateTime.UtcNow;

                await CreateInvoiceHistoryAsync(invoiceId, "Rejected", InvoiceStatus.Freigabe_Erforderlich.ToString(), 
                    InvoiceStatus.Abgelehnt.ToString(), approveDto.Comments, approverId);
            }
            else
            {
                // Check if all required approvals are completed
                var allWorkflows = invoice.ApprovalWorkflows.ToList();
                var pendingWorkflows = allWorkflows.Where(aw => aw.Status == ApprovalStatus.Pending).ToList();

                if (pendingWorkflows.Count == 1 && pendingWorkflows.First().Id == pendingWorkflow.Id)
                {
                    // This was the last pending approval
                    invoice.Status = InvoiceStatus.Freigegeben;
                    invoice.ProcessedBy = approverId;
                    invoice.UpdatedAt = DateTime.UtcNow;

                    await CreateInvoiceHistoryAsync(invoiceId, "Approved", InvoiceStatus.Freigabe_Erforderlich.ToString(), 
                        InvoiceStatus.Freigegeben.ToString(), approveDto.Comments, approverId);
                }
                else
                {
                    await CreateInvoiceHistoryAsync(invoiceId, "Partially Approved", null, null, 
                        approveDto.Comments, approverId);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send notifications
            await _notificationService.NotifyInvoiceApprovalAsync(invoiceId, approveDto.Approved);

            _logger.LogInformation("Invoice {InvoiceId} {Action} by user {UserId}", 
                invoiceId, approveDto.Approved ? "approved" : "rejected", approverId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing approval for invoice {InvoiceId} by user {UserId}", 
                invoiceId, approverId);
            throw;
        }
    }

    public async Task<InvoiceDto?> UpdateInvoiceStatusAsync(int id, InvoiceStatus status, int updatedBy)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return null;

            var oldStatus = invoice.Status.ToString();
            invoice.Status = status;
            invoice.ProcessedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateInvoiceHistoryAsync(id, "Status Changed", oldStatus, status.ToString(), null, updatedBy);

            _logger.LogInformation("Invoice status updated: {InvoiceId} from {OldStatus} to {NewStatus}", 
                id, oldStatus, status);

            return await GetInvoiceByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice status for ID: {InvoiceId}", id);
            throw;
        }
    }

    private async Task CreateInvoiceHistoryAsync(int invoiceId, string action, string? oldStatus, 
        string? newStatus, string? comments, int changedBy)
    {
        var history = new InvoiceHistory
        {
            InvoiceId = invoiceId,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comments = comments,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };

        _context.InvoiceHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Supplier = new SupplierDto
            {
                Id = invoice.Supplier.Id,
                Name = invoice.Supplier.Name,
                Email = invoice.Supplier.Email
            },
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
            Status = invoice.Status.ToString(),
            RequiresApproval = invoice.RequiresApproval,
            ApprovalLevel = invoice.ApprovalLevel,
            AutoApproved = invoice.AutoApproved,
            PdfFilePath = invoice.PdfFilePath,
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
                       invoice.Status != InvoiceStatus.Bezahlt && 
                       invoice.Status != InvoiceStatus.Storniert,
            DaysOverdue = invoice.DueDate < DateTime.UtcNow ? (DateTime.UtcNow - invoice.DueDate).Days : 0,
            PendingApprovals = invoice.ApprovalWorkflows
                .Where(aw => aw.Status == ApprovalStatus.Pending)
                .Select(aw => new ApprovalWorkflowDto
                {
                    Id = aw.Id,
                    InvoiceId = aw.InvoiceId,
                    StepNumber = aw.StepNumber,
                    Approver = new UserDto
                    {
                        Id = aw.Approver.Id,
                        Username = aw.Approver.Username,
                        FirstName = aw.Approver.FirstName,
                        LastName = aw.Approver.LastName
                    },
                    ApprovalLevel = aw.ApprovalLevel,
                    Status = aw.Status.ToString(),
                    CreatedAt = aw.CreatedAt
                })
                .ToArray()
        };
    }
}