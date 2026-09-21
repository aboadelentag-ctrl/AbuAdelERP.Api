using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public class QualityService : IQualityService
    {
        private readonly AppDbContext _context;

        public QualityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> SubmitInspectionAsync(QualityInspectionCreateDto dto)
        {
            // 1. جلب أمر الإنتاج مع كافة تفاصيل الأصناف المندرجة تحته
            var productionOrder = await _context.ProductionOrders
                .Include(o => o.ProductionOrderDetails)
                .FirstOrDefaultAsync(o => o.Id == dto.ProductionOrderId);

            if (productionOrder == null)
                throw new Exception($"عفواً، أمر الإنتاج رقم {dto.ProductionOrderId} غير موجود.");

            // 2. محرك حساب نقاط الجودة
            decimal score = 0;
            if (dto.IsGmpPassed) score += 30;
            if (dto.IsHaccpPassed) score += 40;
            if (dto.IsWeightsPassed) score += 30;

            // 3. اتخاذ القرار الآلي
            string resultStatus;
            string notes = dto.InspectorNotes ?? "";

            if (score == 100)
            {
                resultStatus = "مطابق للمواصفات - ممتاز";
            }
            else if (score >= 70 && dto.IsHaccpPassed)
            {
                resultStatus = "مقبول بشروط";
                notes += " (تم خصم درجات بسبب ملاحظات في النظافة أو الأوزان ولكن المنتج آمن صحياً).";
            }
            else
            {
                resultStatus = "مرفوض - غير صالح";
                notes += " (فشل في تحقيق الحد الأدنى للجودة أو أخطاء حرجة في HACCP).";

                // إيقاف ورفض الأمر وكافة الأصناف التابعة له
                productionOrder.Status = "REJECTED";
                foreach (var detail in productionOrder.ProductionOrderDetails)
                {
                    detail.Status = "REJECTED";
                }
            }

            // تجميع الكميات لتوثيق الفحص الكلي للأمر
            decimal totalQuantity = productionOrder.ProductionOrderDetails.Sum(d => d.ProducedQuantity);
            long primaryItemId = productionOrder.ProductionOrderDetails.FirstOrDefault()?.ItemId ?? 0;

            // 4. توثيق الفحص
            var inspection = new QualityInspection
            {
                CompanyId = 6,
                ItemId = primaryItemId,
                ProductionOrderId = dto.ProductionOrderId,
                InspectionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                InspectionType = "فحص جودة المنتج التام",
                Quantity = totalQuantity,
                Status = "COMPLETED",
                IsGmpPassed = dto.IsGmpPassed,
                IsHaccpPassed = dto.IsHaccpPassed,
                IsWeightsPassed = dto.IsWeightsPassed,
                QualityScore = score,
                InspectorName = dto.InspectorName,
                Notes = notes,
                Result = resultStatus
            };

            await _context.QualityInspections.AddAsync(inspection);
            await _context.SaveChangesAsync();

            return new
            {
                Message = "تم تسجيل فحص الجودة بنجاح",
                OrderNumber = productionOrder.OrderNumber,
                FinalScore = $"{score}%",
                AutomatedDecision = resultStatus,
                InspectorNotes = notes
            };
        }
    }
}