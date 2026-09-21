using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IPurchaseRequestService
    {
        Task<List<PurchaseRequestItemLookupDto>> GetItemsLookupAsync();
        Task<List<PurchaseRequestDepartmentLookupDto>> GetDepartmentsLookupAsync();
        Task<List<PurchaseRequestWarehouseLookupDto>> GetWarehousesLookupAsync();
        Task<List<PurchaseRequestDto>> GetAllRequestsAsync();
        Task<bool> CreateRequestAsync(PurchaseRequestCreateDto dto);
    }

    public class PurchaseRequestService : IPurchaseRequestService
    {
        private readonly AppDbContext _context;

        public PurchaseRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseRequestItemLookupDto>> GetItemsLookupAsync()
        {
            return await _context.Items
                .Where(i => i.Active)
                .Select(i => new PurchaseRequestItemLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Unit = i.Unit // تم ربط الحقل الفعلي للوحدة هنا
                })
                .ToListAsync();
        }

        public async Task<List<PurchaseRequestDepartmentLookupDto>> GetDepartmentsLookupAsync()
        {
            return await _context.Departments
                .Select(d => new PurchaseRequestDepartmentLookupDto
                {
                    Id = d.Id,
                    DepartmentName = d.DepartmentName
                })
                .ToListAsync();
        }

        public async Task<List<PurchaseRequestWarehouseLookupDto>> GetWarehousesLookupAsync()
        {
            return await _context.Warehouses
                .Where(w => w.Active)
                .Select(w => new PurchaseRequestWarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseName = w.WarehouseName
                })
                .ToListAsync();
        }

        public async Task<List<PurchaseRequestDto>> GetAllRequestsAsync()
        {
            return await _context.PurchaseOrders
                .OrderByDescending(p => p.Id)
                .Select(p => new PurchaseRequestDto
                {
                    Id = p.Id,
                    RequestNumber = p.OrderNumber,
                    WarehouseId = p.WarehouseId,
                    WarehouseName = _context.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.WarehouseName).FirstOrDefault() ?? "مخزن غير محدد",
                    RequestDate = p.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Status = p.Status,
                    Notes = string.Empty
                })
                .ToListAsync();
        }

        public async Task<bool> CreateRequestAsync(PurchaseRequestCreateDto dto)
        {
            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            var validSupplierId = await _context.Suppliers
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (validSupplierId == 0)
                throw new Exception("لا يوجد مورد مسجل في قاعدة البيانات لربط الطلب المبدئي به.");

            var validCompanyId = await _context.Companies
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (validCompanyId == 0) validCompanyId = 1;

            string generatedNumber = string.IsNullOrWhiteSpace(dto.RequestNumber)
                ? $"PR-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}"
                : dto.RequestNumber;

            long targetWarehouseId = dto.WarehouseId ?? 1;

            var po = new PurchaseOrder
            {
                CompanyId = validCompanyId,
                SupplierId = validSupplierId,
                WarehouseId = targetWarehouseId,
                OrderNumber = generatedNumber,
                OrderDate = DateOnly.FromDateTime(dto.RequestDate),
                Status = "Pending_Procurement",
                Subtotal = 0,
                DiscountAmount = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            foreach (var line in dto.Lines)
            {
                var poLine = new PurchaseOrderLine
                {
                    PurchaseOrderId = po.Id,
                    ItemId = line.ItemId,
                    Quantity = line.RequestedQuantity,
                    UnitPrice = 0,
                    DiscountAmount = 0,
                    TaxAmount = 0,
                    ReceivedQuantity = 0
                };
                _context.PurchaseOrderLines.Add(poLine);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}