using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IItemHierarchyService
    {
        Task<List<UomConversionDto>> GetAllConversionsAsync();
        Task<bool> CreateConversionAsync(UomConversionCreateDto dto);
        Task<decimal> ConvertUnitAsync(decimal quantity, string fromUnit, string toUnit, long? itemId);

        Task<List<ParentItemLookupDto>> GetParentItemsLookupAsync();
        Task<List<ItemVariantDto>> GetVariantsByParentIdAsync(long parentId);
        Task<List<ItemVariantDto>> GetAllVariantsAsync();
        Task<bool> CreateItemVariantAsync(ItemVariantCreateDto dto);
    }

    public class ItemHierarchyService : IItemHierarchyService
    {
        private readonly AppDbContext _context;

        public ItemHierarchyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UomConversionDto>> GetAllConversionsAsync()
        {
            return await _context.ItemUomConversions
                .OrderByDescending(c => c.Id)
                .Select(c => new UomConversionDto
                {
                    Id = c.Id,
                    ItemId = c.ItemId,
                    ItemName = c.ItemId != null ? _context.Items.Where(i => i.Id == c.ItemId).Select(i => i.ItemName).FirstOrDefault() ?? "" : "تحويل قياسي عام للمصنع",
                    FromUnit = c.FromUnit,
                    ToUnit = c.ToUnit,
                    ConversionFactor = c.ConversionFactor,
                    Notes = c.Notes
                })
                .ToListAsync();
        }

        public async Task<bool> CreateConversionAsync(UomConversionCreateDto dto)
        {
            if (dto == null || dto.ConversionFactor <= 0) return false;

            var conversion = new ItemUomConversion
            {
                CompanyId = 1,
                ItemId = dto.ItemId > 0 ? dto.ItemId : null,
                FromUnit = dto.FromUnit.Trim(),
                ToUnit = dto.ToUnit.Trim(),
                ConversionFactor = dto.ConversionFactor,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ItemUomConversions.Add(conversion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> ConvertUnitAsync(decimal quantity, string fromUnit, string toUnit, long? itemId)
        {
            if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
                return quantity;

            var rule = await _context.ItemUomConversions
                .FirstOrDefaultAsync(c =>
                    (c.ItemId == itemId || c.ItemId == null) &&
                    ((c.FromUnit == fromUnit && c.ToUnit == toUnit) ||
                     (c.FromUnit == toUnit && c.ToUnit == fromUnit)));

            if (rule == null) return quantity;

            if (string.Equals(rule.FromUnit, fromUnit, StringComparison.OrdinalIgnoreCase))
                return quantity * rule.ConversionFactor;
            else
                return quantity / rule.ConversionFactor;
        }

        public async Task<List<ParentItemLookupDto>> GetParentItemsLookupAsync()
        {
            return await _context.Items
                .Where(i => i.Active && i.ParentItemId == null)
                .Select(i => new ParentItemLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Unit = i.Unit
                })
                .ToListAsync();
        }

        public async Task<List<ItemVariantDto>> GetVariantsByParentIdAsync(long parentId)
        {
            return await _context.Items
                .Where(i => i.ParentItemId == parentId)
                .Select(i => new ItemVariantDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit,
                    ParentItemId = i.ParentItemId,
                    ParentItemName = _context.Items.Where(p => p.Id == parentId).Select(p => p.ItemName).FirstOrDefault() ?? "",
                    Active = i.Active
                })
                .ToListAsync();
        }

        public async Task<List<ItemVariantDto>> GetAllVariantsAsync()
        {
            return await _context.Items
                .Where(i => i.ParentItemId != null)
                .OrderByDescending(i => i.Id)
                .Select(i => new ItemVariantDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit,
                    ParentItemId = i.ParentItemId,
                    ParentItemName = _context.Items.Where(p => p.Id == i.ParentItemId).Select(p => p.ItemName).FirstOrDefault() ?? "",
                    Active = i.Active
                })
                .ToListAsync();
        }

        public async Task<bool> CreateItemVariantAsync(ItemVariantCreateDto dto)
        {
            var parent = await _context.Items.FindAsync(dto.ParentItemId);
            if (parent == null) return false;

            var variantItem = new Item
            {
                CompanyId = parent.CompanyId,
                CategoryId = parent.CategoryId,
                ItemType = parent.ItemType,
                ParentItemId = parent.Id,
                ItemCode = string.IsNullOrWhiteSpace(dto.VariantCode) ? $"{parent.ItemCode}-{new Random().Next(10, 99)}" : dto.VariantCode,
                ItemName = dto.VariantName,
                Unit = dto.Unit,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Items.Add(variantItem);
            await _context.SaveChangesAsync();

            // إضافة معامل التعبئة للصنف الفرعي (مثلاً: 1 بلكة = 48 قطعة)
            if (dto.PackingFactor > 0 && !string.IsNullOrWhiteSpace(dto.PackingUnit))
            {
                var packingConversion = new ItemUomConversion
                {
                    CompanyId = parent.CompanyId,
                    ItemId = variantItem.Id,
                    FromUnit = dto.PackingUnit.Trim(),
                    ToUnit = dto.Unit.Trim(),
                    ConversionFactor = dto.PackingFactor,
                    Notes = $"معامل التعبئة القياسي للصنف: {dto.VariantName}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ItemUomConversions.Add(packingConversion);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}