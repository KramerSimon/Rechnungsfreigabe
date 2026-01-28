using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;
using System.Linq.Expressions;


namespace RechnungsfreigabeAPI.Services.Interfaces;

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
