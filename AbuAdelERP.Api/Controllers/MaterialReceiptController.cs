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
    public class MaterialReceiptController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialReceiptController(AppDbContext context)
        {
            _context = context;
        }

        // دالة استلام الخامات وتغذية الأرصدة
        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveMaterials([FromBody] MaterialReceiptRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // الافتراضي للمصنع (يمكن ربطه لاحقاً بشاشة الدخول)
                long companyId = (await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1;
                long warehouseId = request.WarehouseId > 0 ? request.WarehouseId : 1;
                long receiptReference = DateTime.UtcNow.Ticks; // توليد رقم مرجعي فريد لإذن الإضافة

                foreach (var line in request.Lines)
                {
                    if (line.Quantity <= 0) continue; // تجاهل الأسطر الصفرية

                    // 1. تحديث أو إنشاء الرصيد في المخزن
                    var balance = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.WarehouseId == warehouseId && b.ItemId == line.ItemId);

                    if (balance != null)
                    {
                        balance.Quantity += line.Quantity; // إضافة الكمية الواردة للرصيد
                        balance.LastUpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = companyId,
                            WarehouseId = warehouseId,
                            ItemId = line.ItemId,
                            Quantity = line.Quantity,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }

                    // 2. تسجيل الحركة في بطاقة الصنف (Item Ledger)
                    var movement = new StockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = warehouseId,
                        ItemId = line.ItemId,
                        Quantity = line.Quantity, // بالموجب لأنها إضافة
                        MovementType = "PURCHASE_IN", // وارد مشتريات
                        ReferenceType = "RECEIPT_VOUCHER", // إذن إضافة
                        ReferenceId = receiptReference,
                        MovementAt = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(request.SupplierName)
                                ? "إذن إضافة خامات مبدئي (تغذية رصيد)"
                                : $"وارد مشتريات خامات - المورد: {request.SupplierName}"
                    };
                    _context.StockMovements.Add(movement);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "تم استلام الخامات، تحديث الأرصدة، وتسجيل الحركة بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"خطأ أثناء استلام الخامات: {ex.Message}");
            }
        }
    }

    // ==========================================
    // DTO Classes
    // ==========================================
    public class MaterialReceiptRequestDto
    {
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public List<MaterialReceiptLineDto> Lines { get; set; } = new();
    }

    public class MaterialReceiptLineDto
    {
        public long ItemId { get; set; }
        public decimal Quantity { get; set; }
    }
}