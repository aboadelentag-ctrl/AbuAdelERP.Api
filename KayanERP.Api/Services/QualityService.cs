using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public class QualityService : IQualityService
    {
        private readonly AppDbContext _context;

        public QualityService(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. تسجيل واعتماد فحص الجودة واتخاذ القرار الآلي
        // ========================================================
        public async Task<object> SubmitInspectionAsync(QualityInspectionCreateDto dto)
        {
            var productionOrder = await _context.ProductionOrders
                .Include(o => o.ProductionOrderDetails)
                .FirstOrDefaultAsync(o => o.Id == dto.ProductionOrderId);

            if (productionOrder == null)
                throw new InvalidOperationException($"عفواً، أمر الإنتاج رقم {dto.ProductionOrderId} غير موجود.");

            // محرك حساب نقاط الجودة (HACCP 40% - GMP 30% - الأوزان 30%)
            decimal score = 0;
            if (dto.IsGmpPassed) score += 30;
            if (dto.IsHaccpPassed) score += 40;
            if (dto.IsWeightsPassed) score += 30;

            string resultStatus;
            string notes = dto.InspectorNotes ?? "";

            if (score == 100)
            {
                resultStatus = "مطابق للمواصفات - ممتاز";
                productionOrder.Status = "QC_APPROVED";
            }
            else if (score >= 70 && dto.IsHaccpPassed)
            {
                resultStatus = "مقبول بشروط";
                notes += " (تم خصم درجات بسبب ملاحظات في النظافة أو الأوزان ولكن المنتج آمن صحياً).";
                productionOrder.Status = "QC_APPROVED";
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

            decimal totalQuantity = productionOrder.ProductionOrderDetails.Sum(d => d.ProducedQuantity);
            long primaryItemId = productionOrder.ProductionOrderDetails.FirstOrDefault()?.ItemId ?? 0;
            long companyId = productionOrder.CompanyId > 0 ? productionOrder.CompanyId : 1;

            var inspection = new QualityInspection
            {
                CompanyId = companyId,
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
                InspectorName = !string.IsNullOrWhiteSpace(dto.InspectorName) ? dto.InspectorName : "مراقب الجودة",
                Notes = notes.Trim(),
                Result = resultStatus
            };

            await _context.QualityInspections.AddAsync(inspection);
            await _context.SaveChangesAsync();

            return new
            {
                Success = true,
                Message = "تم تسجيل فحص الجودة وتطبيق القرار الرقابي بنجاح",
                OrderNumber = productionOrder.OrderNumber,
                FinalScore = $"{score}%",
                AutomatedDecision = resultStatus,
                InspectorNotes = notes.Trim()
            };
        }

        // ========================================================
        // 2. استعراض كافة تقارير وفحوصات الجودة المسجلة
        // ========================================================
        public async Task<List<QualityInspectionViewDto>> GetAllInspectionsAsync()
        {
            var inspections = await _context.QualityInspections
                .Include(q => q.ProductionOrder)
                .Include(q => q.Item)
                .AsNoTracking()
                .OrderByDescending(q => q.InspectionDate)
                .ToListAsync();

            return inspections.Select(q => new QualityInspectionViewDto
            {
                Id = q.Id,
                ProductionOrderId = q.ProductionOrderId ?? 0,
                OrderNumber = q.ProductionOrder != null ? q.ProductionOrder.OrderNumber : $"PO-#{q.ProductionOrderId}",
                ItemName = q.Item != null ? q.Item.ItemName : "وجبة تشغيل مجمعة",
                InspectionDate = q.InspectionDate,
                QualityScore = q.QualityScore,
                Result = q.Result ?? "قيد المراجعة",
                InspectorName = q.InspectorName ?? "مراقب الجودة",
                IsGmpPassed = q.IsGmpPassed,
                IsHaccpPassed = q.IsHaccpPassed,
                IsWeightsPassed = q.IsWeightsPassed,
                Quantity = q.Quantity,
                Notes = q.Notes
            }).ToList();
        }

        // ========================================================
        // 3. جلب أوامر الإنتاج الجاهزة للفحص المعملي
        // ========================================================
        public async Task<List<PendingProductionOrderDto>> GetPendingOrdersForInspectionAsync()
        {
            var pendingOrders = await _context.ProductionOrders
                .Include(o => o.ProductionOrderDetails).ThenInclude(d => d.Item)
                .AsNoTracking()
                .Where(o => o.Status != "REJECTED" && o.Status != "CANCELLED")
                .OrderByDescending(o => o.Id)
                .Take(50)
                .ToListAsync();

            return pendingOrders.Select(o => new PendingProductionOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                DepartmentName = "صالة الإنتاج",
                Status = o.Status,
                TotalProducedQty = o.ProductionOrderDetails.Sum(d => d.ProducedQuantity),
                ItemsSummary = string.Join("، ", o.ProductionOrderDetails.Select(d => d.Item != null ? d.Item.ItemName : "صنف"))
            }).ToList();
        }
    }

    // ========================================================
    // نماذج البيانات الإضافية لمنظومة الجودة
    // ========================================================
    public class QualityInspectionViewDto
    {
        public long Id { get; set; }
        public long ProductionOrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public DateTime InspectionDate { get; set; }
        public decimal QualityScore { get; set; }
        public string Result { get; set; } = string.Empty;
        public string InspectorName { get; set; } = string.Empty;
        public bool IsGmpPassed { get; set; }
        public bool IsHaccpPassed { get; set; }
        public bool IsWeightsPassed { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class PendingProductionOrderDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalProducedQty { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;
    }
}