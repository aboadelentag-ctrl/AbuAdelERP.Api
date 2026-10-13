using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly AppDbContext _context;

        public WarehouseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync()
        {
            return await _context.Warehouses
                .Include(w => w.Branch)
                .Include(w => w.ParentWarehouse)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Warehouse> AddWarehouseAsync(WarehouseCreateDto dto)
        {
            var companyId = await _context.Companies.Select(c => c.Id).FirstOrDefaultAsync();
            if (companyId <= 0) companyId = 1;

            var branchId = await _context.Branches.Select(b => b.Id).FirstOrDefaultAsync();
            if (branchId <= 0) branchId = 1;

            var newWarehouse = new Warehouse
            {
                WarehouseCode = dto.WarehouseCode?.Trim() ?? string.Empty,
                WarehouseName = dto.WarehouseName?.Trim() ?? string.Empty,
                WarehouseType = dto.WarehouseType?.Trim() ?? "خامات",
                Description = dto.Description?.Trim() ?? string.Empty,
                BranchId = branchId,
                Active = true,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(newWarehouse);
            await _context.SaveChangesAsync();

            return newWarehouse;
        }
    }
}