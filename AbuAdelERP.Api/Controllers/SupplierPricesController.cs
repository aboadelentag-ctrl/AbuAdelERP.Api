using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierPricesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupplierPricesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPrices([FromQuery] long? supplierId = null, [FromQuery] long? itemId = null)
        {
            var query = _context.SupplierItemPrices
                .Include(p => p.Supplier)
                .Include(p => p.Item)
                .AsNoTracking()
                .AsQueryable();

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(p => p.SupplierId == supplierId.Value);

            if (itemId.HasValue && itemId.Value > 0)
                query = query.Where(p => p.ItemId == itemId.Value);

            var list = await query
                .OrderByDescending(p => p.Id)
                .Select(p => new SupplierItemPriceDto
                {
                    Id = p.Id,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "",
                    SupplierCode = p.Supplier != null ? p.Supplier.SupplierCode : "",
                    ItemId = p.ItemId,
                    ItemCode = p.Item != null ? p.Item.ItemCode : "",
                    ItemName = p.Item != null ? p.Item.ItemName : "",
                    Unit = p.Item != null ? p.Item.Unit : "",
                    Price = p.Price,
                    Currency = p.Currency,
                    LeadTimeDays = p.LeadTimeDays,
                    EffectiveDate = p.EffectiveDate,
                    IsPreferred = p.IsPreferred,
                    Notes = p.Notes
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> AddPrice([FromBody] CreateSupplierPriceDto dto)
        {
            if (dto.SupplierId <= 0 || dto.ItemId <= 0 || dto.Price <= 0)
                return BadRequest("يجب اختيار مورد وصنف وتحديد سعر صالح.");

            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
            if (supplier == null) return NotFound("المورد غير موجود.");
            if (!supplier.Active) return BadRequest("لا يمكن تسجيل سعر لمورد موقوف.");

            // إذا تم تعيينه كمورد مفضل، نقوم بإلغاء التفضيل السابق لنفس الصنف
            if (dto.IsPreferred)
            {
                var existingPreferred = await _context.SupplierItemPrices
                    .Where(p => p.ItemId == dto.ItemId && p.IsPreferred)
                    .ToListAsync();

                foreach (var item in existingPreferred)
                {
                    item.IsPreferred = false;
                }
            }

            var record = new SupplierItemPrice
            {
                SupplierId = dto.SupplierId,
                ItemId = dto.ItemId,
                Price = dto.Price,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency,
                LeadTimeDays = dto.LeadTimeDays > 0 ? dto.LeadTimeDays : 1,
                EffectiveDate = dto.EffectiveDate != default ? dto.EffectiveDate : DateOnly.FromDateTime(DateTime.UtcNow),
                IsPreferred = dto.IsPreferred,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.SupplierItemPrices.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تسجيل سعر التوريد بنجاح." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrice(long id)
        {
            var record = await _context.SupplierItemPrices.FindAsync(id);
            if (record == null) return NotFound("السعر غير موجود.");

            _context.SupplierItemPrices.Remove(record);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم حذف السعر بنجاح." });
        }
    }

    public class SupplierItemPriceDto
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public int LeadTimeDays { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSupplierPriceDto
    {
        public long SupplierId { get; set; }
        public long ItemId { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public int LeadTimeDays { get; set; } = 1;
        public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
    }
}