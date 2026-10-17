using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public interface ISupplierPriceService
    {
        Task<List<SupplierItemPriceDto>> GetAllPricesAsync(long? supplierId = null, long? itemId = null);
        Task<SupplierPriceOperationResultDto> AddOrUpdatePriceAsync(CreateSupplierPriceDto dto);
        Task<bool> UpdatePriceAsync(long id, CreateSupplierPriceDto dto);
        Task<bool> DeletePriceAsync(long id);
    }

    public class SupplierPriceService : ISupplierPriceService
    {
        private readonly AppDbContext _context;

        public SupplierPriceService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. استعلام كافة أسعار التوريد مع الفلترة
        // ==============================================================
        public async Task<List<SupplierItemPriceDto>> GetAllPricesAsync(long? supplierId = null, long? itemId = null)
        {
            var query = _context.SupplierItemPrices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.Item)
                .AsQueryable();

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(p => p.SupplierId == supplierId.Value);

            if (itemId.HasValue && itemId.Value > 0)
                query = query.Where(p => p.ItemId == itemId.Value);

            return await query
                .OrderByDescending(p => p.Id)
                .Select(p => new SupplierItemPriceDto
                {
                    Id = p.Id,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : string.Empty,
                    SupplierCode = p.Supplier != null ? p.Supplier.SupplierCode : string.Empty,
                    ItemId = p.ItemId,
                    ItemCode = p.Item != null ? p.Item.ItemCode : string.Empty,
                    ItemName = p.Item != null ? p.Item.ItemName : string.Empty,
                    Unit = p.Item != null ? (p.Item.Unit ?? string.Empty) : string.Empty,
                    Price = p.Price,
                    Currency = p.Currency,
                    LeadTimeDays = p.LeadTimeDays,
                    DeliveryDays = p.LeadTimeDays,
                    EffectiveDate = p.EffectiveDate.ToString("yyyy-MM-dd"),
                    IsPreferred = p.IsPreferred,
                    Notes = p.Notes
                })
                .ToListAsync();
        }

        // ==============================================================
        // 2. تسجيل أو تحديث سعر التوريد مع ضبط المورد المفضل ذرياً
        // ==============================================================
        public async Task<SupplierPriceOperationResultDto> AddOrUpdatePriceAsync(CreateSupplierPriceDto dto)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
            if (supplier == null)
                throw new InvalidOperationException("المورد غير موجود.");

            if (!supplier.Active)
                throw new InvalidOperationException("لا يمكن تسجيل سعر لمورد موقوف.");

            DateOnly parsedDate = ParseDate(dto.EffectiveDate);
            int days = dto.LeadTimeDays > 0 ? dto.LeadTimeDays : (dto.DeliveryDays > 0 ? dto.DeliveryDays : 1);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // إذا تم تعيينه كمورد مفضل، إلغاء التفضيل عن بقية الموردين لنفس الصنف
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
                    await transaction.CommitAsync();

                    return new SupplierPriceOperationResultDto
                    {
                        Success = true,
                        Message = "تم تحديث سعر التوريد المسجل مسبقاً بنجاح.",
                        Id = existingRecord.Id
                    };
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
                await transaction.CommitAsync();

                return new SupplierPriceOperationResultDto
                {
                    Success = true,
                    Message = "تم تسجيل سعر التوريد بنجاح.",
                    Id = record.Id
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 3. تعديل سعر توريد قائم
        // ==============================================================
        public async Task<bool> UpdatePriceAsync(long id, CreateSupplierPriceDto dto)
        {
            var record = await _context.SupplierItemPrices.FindAsync(id);
            if (record == null) return false;

            DateOnly parsedDate = ParseDate(dto.EffectiveDate);
            int days = dto.LeadTimeDays > 0 ? dto.LeadTimeDays : (dto.DeliveryDays > 0 ? dto.DeliveryDays : record.LeadTimeDays);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 4. حذف سعر توريد
        // ==============================================================
        public async Task<bool> DeletePriceAsync(long id)
        {
            var record = await _context.SupplierItemPrices.FindAsync(id);
            if (record == null) return false;

            _context.SupplierItemPrices.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        private static DateOnly ParseDate(string? dateString)
        {
            if (!string.IsNullOrWhiteSpace(dateString))
            {
                if (DateOnly.TryParse(dateString, out var dOnly))
                    return dOnly;
                if (DateTime.TryParse(dateString, out var dt))
                    return DateOnly.FromDateTime(dt);
            }
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}