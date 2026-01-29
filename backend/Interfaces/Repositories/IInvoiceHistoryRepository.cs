using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IInvoiceHistoryRepository : IRepository<InvoiceHistory>
{
    Task<IEnumerable<InvoiceHistory>> GetByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<InvoiceHistoryTimelineDto>> GetTimelineByInvoiceIdAsync(int invoiceId);
}
