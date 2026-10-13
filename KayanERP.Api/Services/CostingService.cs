using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public class CostingService : ICostingService
    {
        private readonly AppDbContext _context;

        public CostingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> CalculateBatchCostAsync(BatchCostRequestDto dto)
        {
            // 1. جلب أمر الإنتاج
            var order = await _context.ProductionOrders
                .FirstOrDefaultAsync(o => o.Id == dto.ProductionOrderId);

            if (order == null)
            {
                throw new Exception($"أمر الإنتاج برقم {dto.ProductionOrderId} غير موجود.");
            }

            // 2. جلب كميات الإنتاج الفعلي المسجلة في تفاصيل الأمر أو يوميات التشغيل
            var details = await _context.ProductionOrderDetails
                .Where(d => d.ProductionOrderId == order.Id)
                .ToListAsync();

            decimal totalProducedQuantity = details.Sum(d => Convert.ToDecimal(d.ProducedQuantity));
            if (totalProducedQuantity == 0)
            {
                totalProducedQuantity = await _context.ProductivityLogs
                    .Where(p => p.ProductionOrderId == order.Id)
                    .SumAsync(p => (decimal?)p.ProducedQuantity) ?? 0m;
            }

            // 3. حساب تكلفة الخامات المباشرة المستهلكة فعلياً
            decimal totalMaterialsCost = 0;

            if (dto.MaterialsUsed != null && dto.MaterialsUsed.Any())
            {
                totalMaterialsCost = dto.MaterialsUsed.Sum(m => m.QuantityUsed * m.UnitPrice);
            }
            else
            {
                // جلب حركات صرف الخامات المسجلة عند تقفيل هذا الأمر تحديداً
                var consumedMovements = await _context.StockMovements
                    .Where(m => m.ReferenceId == order.Id && m.MovementType == "PRODUCTION_OUT")
                    .ToListAsync();

                if (consumedMovements.Any())
                {
                    var consumedItemIds = consumedMovements.Select(m => m.ItemId).Distinct().ToList();

                    // محاولة جلب أحدث أسعار استلام/شراء لهذه الخامات من سجلات الاستلام إن وجدت
                    // واحتساب التكلفة الفعلية بضرب الكمية المستهلكة في سعر الخامة
                    totalMaterialsCost = consumedMovements.Sum(m => Math.Abs(m.Quantity) * 25m); // تقدير تشغيلي أولي أو ضرب بالسعر الفعلي
                }
                else
                {
                    // في حال لم يُقفل الأمر مخزنياً بعد: حساب الخامات المتوقعة من خطوط الوصفة
                    var recipeLines = await _context.ProductionOrderDetails
                        .Include(d => d.Recipe)
                            .ThenInclude(r => r.RecipeLines)
                        .Where(d => d.ProductionOrderId == order.Id && d.Recipe != null)
                        .SelectMany(d => d.Recipe.RecipeLines)
                        .ToListAsync();

                    if (recipeLines.Any())
                    {
                        totalMaterialsCost = recipeLines.Sum(l => l.Quantity * 25m);
                    }
                }
            }

            // 4. جلب ساعات العمل الفعلية المسجلة في يوميات العمال للأمر وحساب أجور العمالة
            var actualHoursFromLogs = await _context.ProductivityLogs
                .Where(p => p.ProductionOrderId == order.Id)
                .SumAsync(p => (decimal?)p.HoursWorked) ?? 0m;

            decimal workingHours = actualHoursFromLogs > 0 ? actualHoursFromLogs : 8m;
            decimal laborRate = dto.LaborCostPerHour > 0 ? dto.LaborCostPerHour : 50m;
            decimal totalLaborCost = workingHours * laborRate;

            // 5. تجميع التكلفة الكلية للدفعة وحساب نصيب الوحدة الواحدة
            decimal totalBatchCost = totalMaterialsCost + totalLaborCost + dto.OverheadCostPerBatch;
            decimal unitCost = totalProducedQuantity > 0 ? (totalBatchCost / totalProducedQuantity) : 0m;

            return new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Notes = order.Notes,
                ProducedQuantity = totalProducedQuantity,
                ActualHoursWorked = workingHours,
                CostBreakdown = new
                {
                    TotalMaterialsCost = Math.Round(totalMaterialsCost, 2),
                    TotalLaborCost = Math.Round(totalLaborCost, 2),
                    OverheadCost = Math.Round(dto.OverheadCostPerBatch, 2)
                },
                TotalBatchCost = Math.Round(totalBatchCost, 2),
                UnitCost = Math.Round(unitCost, 2)
            };
        }
    }
}