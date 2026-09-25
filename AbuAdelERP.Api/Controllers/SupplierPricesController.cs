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

        // ==============================================================
        // 1. جلب كافة أسعار التوريد (GET: api/SupplierPrices)
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllPrices([FromQuery] long? supplierId = null, [FromQuery] long? itemId = null)
        {
            try
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
                        DeliveryDays = p.LeadTimeDays,
                        EffectiveDate = p.EffectiveDate.ToString("yyyy-MM-dd"),
                        IsPreferred = p.IsPreferred,
                        Notes = p.Notes
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأسعار: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. تسجيل سعر توريد جديد أو تحديث السعر الحالي (POST: api/SupplierPrices)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> AddPrice([FromBody] CreateSupplierPriceDto dto)
        {
            if (dto == null || dto.SupplierId <= 0 || dto.ItemId <= 0 || dto.Price <= 0)
                return BadRequest("يجب اختيار مورد وصنف وتحديد سعر توريد أكبر من الصفر.");

            try
            {
                var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
                if (supplier == null) return NotFound("المورد غير موجود.");
                if (!supplier.Active) return BadRequest("لا يمكن تسجيل سعر لمورد موقوف.");

                // تحويل التاريخ بأمان لتفادي أخطاء DateOnly
                DateOnly parsedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!string.IsNullOrWhiteSpace(dto.EffectiveDate))
                {
                    if (DateOnly.TryParse(dto.EffectiveDate, out var dOnly))
                        parsedDate = dOnly;
                    else if (DateTime.TryParse(dto.EffectiveDate, out var dt))
                        parsedDate = DateOnly.FromDateTime(dt);
                }

                int days = dto.LeadTimeDays > 0 ? dto.LeadTimeDays : (dto.DeliveryDays > 0 ? dto.DeliveryDays : 1);

                // إدارة تفضيل المورد للصنف
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

                // فحص هل السعر مسجل مسبقاً لتحديثه بدلاً من تكراره
                var existingRecord = await _context.SupplierItemPrices
                    .FirstOrDefaultAsync(p => p.SupplierId == dto.SupplierId && p.ItemId == dto.ItemId);

                if (existingRecord != null)
                {
                    existingRecord.Price = dto.Price;
                    existingRecord.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency;
                    existingRecord.LeadTimeDays = days;
                    existingRecord.EffectiveDate = parsedDate;
                    existingRecord.IsPreferred = dto.IsPreferred;
                    existingRecord.Notes = dto.Notes;

                    await _context.SaveChangesAsync();
                    return Ok(new { Message = "تم تحديث سعر التوريد المسجل مسبقاً بنجاح.", Id = existingRecord.Id });
                }

                var record = new SupplierItemPrice
                {
                    SupplierId = dto.SupplierId,
                    ItemId = dto.ItemId,
                    Price = dto.Price,
                    Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency,
                    LeadTimeDays = days,
                    EffectiveDate = parsedDate,
                    IsPreferred = dto.IsPreferred,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SupplierItemPrices.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم تسجيل سعر التوريد بنجاح.", Id = record.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ السعر: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تعديل سعر توريد قائم (PUT: api/SupplierPrices/{id})
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePrice(long id, [FromBody] CreateSupplierPriceDto dto)
        {
            if (dto == null || dto.Price <= 0)
                return BadRequest("بيانات غير صالحة.");

            try
            {
                var record = await _context.SupplierItemPrices.FindAsync(id);
                if (record == null) return NotFound("سعر التوريد غير موجود.");

                DateOnly parsedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!string.IsNullOrWhiteSpace(dto.EffectiveDate))
                {
                    if (DateOnly.TryParse(dto.EffectiveDate, out var dOnly))
                        parsedDate = dOnly;
                    else if (DateTime.TryParse(dto.EffectiveDate, out var dt))
                        parsedDate = DateOnly.FromDateTime(dt);
                }

                int days = dto.LeadTimeDays > 0 ? dto.LeadTimeDays : (dto.DeliveryDays > 0 ? dto.DeliveryDays : record.LeadTimeDays);

                if (dto.IsPreferred)
                {
                    var otherPreferred = await _context.SupplierItemPrices
                        .Where(p => p.ItemId == dto.ItemId && p.IsPreferred && p.Id != id)
                        .ToListAsync();

                    foreach (var item in otherPreferred)
                    {
                        item.IsPreferred = false;
                    }
                }

                record.SupplierId = dto.SupplierId > 0 ? dto.SupplierId : record.SupplierId;
                record.ItemId = dto.ItemId > 0 ? dto.ItemId : record.ItemId;
                record.Price = dto.Price;
                record.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency;
                record.LeadTimeDays = days;
                record.EffectiveDate = parsedDate;
                record.IsPreferred = dto.IsPreferred;
                record.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تحديث سعر التوريد بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء التعديل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. حذف سعر توريد (DELETE: api/SupplierPrices/{id})
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrice(long id)
        {
            try
            {
                var record = await _context.SupplierItemPrices.FindAsync(id);
                if (record == null) return NotFound("السعر غير موجود.");

                _context.SupplierItemPrices.Remove(record);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم حذف السعر بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"تعذر الحذف: {ex.GetBaseException().Message}");
            }
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
        public int DeliveryDays { get; set; }
        public string EffectiveDate { get; set; } = string.Empty;
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
        public int DeliveryDays { get; set; } = 1;
        public string? EffectiveDate { get; set; }
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
    }
}