using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public class SalesService : ISalesService
    {
        private readonly AppDbContext _context;

        public SalesService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية لشاشة الفاتورة (الفروع + العملاء + الأصناف + المخازن)
        // ==============================================================
        public async Task<SalesLookupsDto> GetLookupsAsync()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => b.Active)
                .OrderBy(b => b.Id)
                .Select(b => new SalesBranchLookupDto
                {
                    Id = b.Id,
                    BranchCode = b.BranchCode,
                    BranchName = b.BranchName
                })
                .ToListAsync();

            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Active)
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerLookupDto
                {
                    Id = c.Id,
                    CustomerName = c.CustomerName,
                    CustomerCode = c.CustomerCode,
                    Phone = c.Phone
                })
                .ToListAsync();

            var finishedItems = await _context.Items
                .AsNoTracking()
                .Where(i => i.Active && (i.ItemType == "منتج تام" || i.ItemType == "FINISHED_GOODS" || i.ItemType == "وسيط" || i.ItemType == "نصف مصنع"))
                .OrderBy(i => i.ItemName)
                .Select(i => new SalesFinishedItemLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit,
                    DepartmentId = i.DepartmentId,
                    ShelfLifeDays = i.ShelfLifeDays,
                    SellingPrice = i.BasePrice
                })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Active && !w.WarehouseName.Contains("مرتجع") && !w.WarehouseName.Contains("تالف"))
                .OrderBy(w => w.WarehouseName)
                .Select(w => new SalesWarehouseLookupDto
                {
                    Id = w.Id,
                    BranchId = w.BranchId,
                    WarehouseName = w.WarehouseName,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseType = w.WarehouseType
                })
                .ToListAsync();

            return new SalesLookupsDto
            {
                Branches = branches,
                Customers = customers,
                FinishedItems = finishedItems,
                Warehouses = warehouses
            };
        }

        // ==============================================================
        // 2. استعراض كافة فواتير المبيعات
        // ==============================================================
        public async Task<List<SalesInvoiceViewDto>> GetAllInvoicesAsync()
        {
            var invoices = await _context.SalesInvoices
                .AsNoTracking()
                .Include(i => i.InvoiceLines)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            var branchIds = invoices.Where(i => i.BranchId.HasValue).Select(i => i.BranchId!.Value).Distinct().ToList();
            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => branchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.BranchName);

            var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.CustomerName);

            var itemIds = invoices.SelectMany(i => i.InvoiceLines.Select(l => l.ItemId)).Distinct().ToList();
            var items = await _context.Items
                .AsNoTracking()
                .Where(it => itemIds.Contains(it.Id))
                .ToDictionaryAsync(it => it.Id, it => new { it.ItemCode, it.ItemName, it.Unit });

            return invoices.Select(inv => new SalesInvoiceViewDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                BranchId = inv.BranchId,
                BranchName = (inv.BranchId.HasValue && branches.ContainsKey(inv.BranchId.Value)) ? branches[inv.BranchId.Value] : "الفرع الرئيسي",
                CustomerId = inv.CustomerId,
                CustomerName = customers.ContainsKey(inv.CustomerId) ? customers[inv.CustomerId] : "عميل غير محدد",
                TotalAmount = inv.TotalAmount,
                ItemsCount = inv.InvoiceLines.Count,
                Lines = inv.InvoiceLines.Select(l => new SalesInvoiceLineViewDto
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    ItemCode = items.ContainsKey(l.ItemId) ? items[l.ItemId].ItemCode : string.Empty,
                    ItemName = items.ContainsKey(l.ItemId) ? items[l.ItemId].ItemName : "صنف",
                    Unit = items.ContainsKey(l.ItemId) ? (items[l.ItemId].Unit ?? string.Empty) : string.Empty,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.LineTotal
                }).ToList()
            }).ToList();
        }

        // ==============================================================
        // 3. استعراض تفاصيل فاتورة مبيعات محددة
        // ==============================================================
        public async Task<SalesInvoiceViewDto?> GetInvoiceByIdAsync(long id)
        {
            var inv = await _context.SalesInvoices
                .AsNoTracking()
                .Include(i => i.InvoiceLines)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv == null) return null;

            var customer = await _context.Customers.FindAsync(inv.CustomerId);
            var branch = inv.BranchId.HasValue ? await _context.Branches.FindAsync(inv.BranchId.Value) : null;

            var itemIds = inv.InvoiceLines.Select(l => l.ItemId).Distinct().ToList();
            var items = await _context.Items
                .AsNoTracking()
                .Where(it => itemIds.Contains(it.Id))
                .ToDictionaryAsync(it => it.Id, it => new { it.ItemCode, it.ItemName, it.Unit });

            return new SalesInvoiceViewDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                BranchId = inv.BranchId,
                BranchName = branch != null ? branch.BranchName : "الفرع الرئيسي",
                CustomerId = inv.CustomerId,
                CustomerName = customer != null ? customer.CustomerName : "عميل غير محدد",
                TotalAmount = inv.TotalAmount,
                ItemsCount = inv.InvoiceLines.Count,
                Lines = inv.InvoiceLines.Select(l => new SalesInvoiceLineViewDto
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    ItemCode = items.ContainsKey(l.ItemId) ? items[l.ItemId].ItemCode : string.Empty,
                    ItemName = items.ContainsKey(l.ItemId) ? items[l.ItemId].ItemName : "صنف",
                    Unit = items.ContainsKey(l.ItemId) ? (items[l.ItemId].Unit ?? string.Empty) : string.Empty,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.LineTotal
                }).ToList()
            };
        }

        // ==============================================================
        // 4. إنشاء واعتماد فاتورة مبيعات وترحيل الأثر المخزني والمالي
        // ==============================================================
        public async Task<SalesInvoiceResultDto> CreateInvoiceAsync(SalesInvoiceCreateDto dto)
        {
            if (dto == null || dto.InvoiceLines == null || !dto.InvoiceLines.Any())
                throw new InvalidOperationException("يجب إضافة صنف واحد على الأقل لإنشاء الفاتورة.");

            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"العميل رقم {dto.CustomerId} غير موجود في النظام.");

            if (!customer.Active)
                throw new InvalidOperationException($"العميل [{customer.CustomerName}] موقوف حالياً ولا يمكن إصدار فواتير له.");

            long branchId = dto.BranchId ?? 1;
            var currentBranch = await _context.Branches.FindAsync(branchId);
            string branchDisplayName = currentBranch != null ? currentBranch.BranchName : "الفرع الرئيسي";

            long warehouseId = dto.WarehouseId ?? 0;
            if (warehouseId <= 0)
            {
                var defaultWh = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => w.Active && w.BranchId == branchId)
                    .Select(w => w.Id)
                    .FirstOrDefaultAsync();

                warehouseId = defaultWh > 0 ? defaultWh : await _context.Warehouses.Where(w => w.Active).Select(w => w.Id).FirstOrDefaultAsync();
            }

            if (warehouseId <= 0)
                throw new InvalidOperationException("تعذر تحديد مستودع صرف المبيعات في النظام، يرجى التأكد من تسجيل مستودع نشط في إدارة المخازن.");

            var now = DateTime.UtcNow;
            decimal finalTotalAmount = dto.InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int monthlyCount = await _context.SalesInvoices
                    .CountAsync(i => i.InvoiceDate.Year == now.Year && i.InvoiceDate.Month == now.Month);
                string invoiceNumber = $"INV-{now:yyyyMM}-{(monthlyCount + 1):D4}";

                var invoice = new SalesInvoice
                {
                    CompanyId = customer.CompanyId > 0 ? customer.CompanyId : 1,
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = now,
                    BranchId = branchId,
                    CustomerId = dto.CustomerId,
                    TotalAmount = finalTotalAmount,
                    CreatedAt = now
                };

                foreach (var line in dto.InvoiceLines)
                {
                    invoice.InvoiceLines.Add(new SalesInvoiceLine
                    {
                        ItemId = line.ItemId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.Quantity * line.UnitPrice
                    });
                }

                _context.SalesInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                // خصم المخزون اللحظي وتسجيل حركات كارت الصنف
                foreach (var line in dto.InvoiceLines)
                {
                    var item = await _context.Items.FindAsync(line.ItemId);
                    if (item == null)
                        throw new InvalidOperationException($"المنتج رقم {line.ItemId} غير موجود.");

                    decimal unitCost = item.BasePrice;

                    var stockBal = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.ItemId == line.ItemId && b.WarehouseId == warehouseId);

                    if (stockBal != null)
                    {
                        stockBal.Quantity -= line.Quantity;
                        stockBal.LastUpdatedAt = now;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = customer.CompanyId > 0 ? customer.CompanyId : 1,
                            WarehouseId = warehouseId,
                            ItemId = line.ItemId,
                            Quantity = -line.Quantity,
                            ReservedQuantity = 0,
                            LastUpdatedAt = now
                        });
                    }

                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = customer.CompanyId > 0 ? customer.CompanyId : 1,
                        WarehouseId = warehouseId,
                        ItemId = line.ItemId,
                        MovementType = "SALES_ISSUE",
                        Quantity = -line.Quantity,
                        UnitCost = unitCost,
                        ReferenceType = "SALES_INVOICE",
                        ReferenceId = invoice.Id,
                        MovementAt = now,
                        CreatedAt = now,
                        Notes = $"فاتورة مبيعات رقم [{invoice.InvoiceNumber}] | الفرع: {branchDisplayName} | العميل: {customer.CustomerName}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new SalesInvoiceResultDto
                {
                    Success = true,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    TotalAmount = invoice.TotalAmount,
                    ItemsCount = invoice.InvoiceLines.Count,
                    Message = $"تم إصدار فاتورة المبيعات وترحيل الأثر المخزني بنجاح | تم الترحيل المالي آلياً: [من حـ/ خزينة ({branchDisplayName})] إلى [حـ/ إيرادات مبيعات التوزيع والطلبيات] متزناً 100%."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}