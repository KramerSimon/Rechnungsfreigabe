using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Interfaces.Repositories;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Interfaces.Repositories.Implementations;

public class InvoiceHistoryRepository : Repository<InvoiceHistory>, IInvoiceHistoryRepository
{
    public InvoiceHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<InvoiceHistory>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(ih => ih.InvoiceId == invoiceId)
            .OrderByDescending(ih => ih.ChangedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<InvoiceHistoryTimelineDto>> GetTimelineByInvoiceIdAsync(int invoiceId)
    {
        var entries = await GetByInvoiceIdAsync(invoiceId);
        if (!entries.Any())
            return Enumerable.Empty<InvoiceHistoryTimelineDto>();

        return new[] { new InvoiceHistoryTimelineDto
        {
            Entries = entries.Select(ih => new InvoiceHistoryDto
            {
                Id = ih.Id,
                InvoiceId = ih.InvoiceId,
                Action = ih.Action,
                OldStatus = ih.OldStatus,
                NewStatus = ih.NewStatus,
                ChangedByUser = ih.ChangedByUser != null ? new UserDto { Id = ih.ChangedByUser.Id, FirstName = ih.ChangedByUser.FirstName, LastName = ih.ChangedByUser.LastName } : null,
                ChangedAt = ih.ChangedAt
            }).ToList()
        } };
    }
}
