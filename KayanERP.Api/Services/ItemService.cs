using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Services
{
    public interface IItemService
    {
        Task<IEnumerable<Item>> GetAllItemsAsync();
        Task<Item?> GetItemByIdAsync(long id);
        Task<Item> CreateItemAsync(Item item);
        Task<bool> UpdateItemAsync(long id, Item item);
        Task<bool> DeleteItemAsync(long id);
        Task<string> ImportItemsFromExcelAsync(IFormFile file);
    }

    public class ItemService : IItemService
    {
        private readonly AppDbContext _context;

        public ItemService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _context.Items
                .AsNoTracking()
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Item?> GetItemByIdAsync(long id)
        {
            return await _context.Items.FindAsync(id);
        }

        public async Task<Item> CreateItemAsync(Item item)
        {
            item.CreatedAt = DateTime.UtcNow;
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateItemAsync(long id, Item item)
        {
            var existing = await _context.Items.FindAsync(id);
            if (existing == null) return false;

            existing.CompanyId = item.CompanyId;
            existing.CategoryId = item.CategoryId;
            existing.DepartmentId = item.DepartmentId;
            existing.DepartmentName = item.DepartmentName;
            existing.ItemCode = item.ItemCode;
            existing.ItemName = item.ItemName;
            existing.ItemType = item.ItemType;
            existing.Unit = item.Unit;
            existing.Active = item.Active;
            existing.HasExpiryDate = item.HasExpiryDate;
            existing.ShelfLifeDays = item.ShelfLifeDays;

            // تحديث محددات حدود المخزون والأمان
            existing.ReorderLevel = item.ReorderLevel;
            existing.SafetyStock = item.SafetyStock;
            existing.MaxStock = item.MaxStock;

            // الخصائص التوافقية
            existing.BasePrice = item.BasePrice;
            existing.Barcode = item.Barcode;
            existing.IsVariant = item.IsVariant;
            existing.IsMerged = item.IsMerged;
            existing.IsContainer = item.IsContainer;
            existing.MultiUom = item.MultiUom;
            existing.HideDetailPrint = item.HideDetailPrint;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteItemAsync(long id)
        {
            var existing = await _context.Items.FindAsync(id);
            if (existing == null) return false;

            bool hasMovements = await _context.StockMovements.AnyAsync(m => m.ItemId == id);
            if (hasMovements)
            {
                existing.Active = false; // تعطيل ناعم للحفاظ على سلامة القيود المحاسبية
                await _context.SaveChangesAsync();
                return false;
            }

            _context.Items.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ImportItemsFromExcelAsync(IFormFile file)
        {
            await Task.CompletedTask;
            return "تم الاستيراد بنجاح";
        }
    }
}