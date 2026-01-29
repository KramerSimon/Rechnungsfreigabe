using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

using RechnungsfreigabeAPI.Interfaces.Services;
namespace RechnungsfreigabeAPI.Interfaces.Services.Implementations;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IUnitOfWork unitOfWork;
    public PurchaseOrderService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
        }

    public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
    {
        var storniertStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.PurchaseOrder.Storniert, 
            EntityTypes.PurchaseOrder);
        
        var allPurchaseOrders = await unitOfWork.PurchaseOrders.GetAllAsync();
        var purchaseOrders = allPurchaseOrders
            .Where(po => po.StatusId != storniertStatus?.Id)
            .OrderByDescending(po => po.CreatedAt);

        return purchaseOrders.Select(MapToDto);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(string id)
    {
        var purchaseOrder = await unitOfWork.PurchaseOrders.GetByNumberAsync(id);
        return purchaseOrder != null ? MapToDto(purchaseOrder) : null;
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto createDto, int createdBy)
    {
        var offenStatus = await unitOfWork.Statuses.GetByCodeAndTypeAsync(
            RechnungsfreigabeAPI.Models.StatusCodes.PurchaseOrder.Offen, 
            EntityTypes.PurchaseOrder);
        
        var purchaseOrder = new PurchaseOrder
        {
            Id = createDto.Id,
            Title = createDto.Title,
            Description = createDto.Description,
            CostCenterId = createDto.CostCenterId,
            ProjectId = createDto.ProjectId,
            TotalAmount = createDto.TotalAmount,
            Currency = createDto.Currency,
            StatusId = offenStatus?.Id,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        unitOfWork.PurchaseOrders.Add(purchaseOrder);
        await unitOfWork.SaveChangesAsync();

        return await GetPurchaseOrderByIdAsync(purchaseOrder.Id) 
            ?? throw new InvalidOperationException("Failed to retrieve created purchase order");
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            Title = po.Title,
            Description = po.Description,
            CostCenterId = po.CostCenterId,
            CostCenterName = po.CostCenter?.Name,
            ProjectId = po.ProjectId,
            ProjectName = po.Project?.Name,
            TotalAmount = po.TotalAmount,
            Currency = po.Currency,
            Status = po.Status?.ToString() ?? string.Empty,
            Creator = po.Creator != null ? new UserDto
            {
                Id = po.Creator.Id,
                Username = po.Creator.Username,
                FirstName = po.Creator.FirstName,
                LastName = po.Creator.LastName
            } : null,
            Approver = po.Approver != null ? new UserDto
            {
                Id = po.Approver.Id,
                Username = po.Approver.Username,
                FirstName = po.Approver.FirstName,
                LastName = po.Approver.LastName
            } : null,
            CreatedAt = po.CreatedAt,
            ApprovedAt = po.ApprovedAt
        };
    }
}
