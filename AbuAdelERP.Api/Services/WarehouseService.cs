using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public class WarehouseService : IWarehouseService
{
    private readonly AppDbContext _context;

    public WarehouseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Warehouse>> GetAllWarehousesAsync()
    {
        return await _context.Warehouses.ToListAsync();
    }

    public async Task<Warehouse> AddWarehouseAsync(WarehouseCreateDto dto)
    {
        var newWarehouse = new Warehouse
        {
            WarehouseCode = dto.WarehouseCode,
            WarehouseName = dto.WarehouseName,
            WarehouseType = dto.WarehouseType,
            Description = dto.Description,

            // بيانات يتحكم بها النظام (تمت إضافة الفرع)
            BranchId = 1,    // <--- إجبار المخزن على التبعية للفرع رقم 1 لتخطي قيد قاعدة البيانات
            Active = true,
            CompanyId = 6,
            CreatedAt = DateTime.UtcNow
        };

        _context.Warehouses.Add(newWarehouse);
        await _context.SaveChangesAsync();

        return newWarehouse;
    }
}