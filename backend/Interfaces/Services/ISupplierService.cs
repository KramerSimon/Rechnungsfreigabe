using Microsoft.EntityFrameworkCore;
using RechnungsfreigabeAPI.Data;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;


namespace RechnungsfreigabeAPI.Interfaces.Services;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto createSupplierDto);
    Task<SupplierDto?> UpdateSupplierAsync(int id, CreateSupplierDto updateSupplierDto);
    Task<bool> DeleteSupplierAsync(int id);
    Task<PagedResult<SupplierDto>> GetSuppliersPagedAsync(PageRequest pageRequest);
}
