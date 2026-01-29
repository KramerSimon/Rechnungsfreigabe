using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByIdWithIncludesAsync(int id);
    Task<Invoice?> GetByIdWithFullDetailsAsync(int id);
    IQueryable<Invoice> GetAllWithFullDetailsQuery();
    Task<IEnumerable<Invoice>> GetByStatusAsync(string statusCode);
    Task<IEnumerable<Invoice>> GetPendingAsync();
    Task<Invoice?> GetWithRelationsAsync(int invoiceId);
    Task<Invoice?> GetForEscalationAsync(int invoiceId);
}
