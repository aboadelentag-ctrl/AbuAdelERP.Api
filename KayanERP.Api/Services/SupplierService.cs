using KayanERP.Api.DTOs;
using KayanERP.Api.Models;
using KayanERP.UI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KayanERP.Api.Services
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllSuppliersAsync();
        Task<bool> CreateSupplierAsync(SupplierCreateDto dto);
    }

    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;

        public SupplierService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupplierDto>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    CreditLimit = s.CreditLimit,
                    Active = s.Active
                })
                .ToListAsync();
        }

        public async Task<bool> CreateSupplierAsync(SupplierCreateDto dto)
        {
            if (dto == null) return false;

            var supplier = new Supplier
            {
                CompanyId = dto.CompanyId,
                SupplierCode = dto.SupplierCode,
                SupplierName = dto.SupplierName,
                CreditLimit = dto.CreditLimit,
                Active = dto.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}