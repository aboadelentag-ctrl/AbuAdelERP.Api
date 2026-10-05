using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialRequisitionsController : ControllerBase
    {
        private readonly IMaterialRequisitionService _service;
        private readonly AppDbContext _context;

        public MaterialRequisitionsController(IMaterialRequisitionService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // ========================================================
        // 1. القوائم المرجعية: حصر المخازن والصالات
        // ========================================================
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            try
            {
                var departments = await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.DepartmentName.Contains("قسم")
                             || d.DepartmentName.Contains("صالة")
                             || d.DepartmentName.Contains("تورت")
                             || d.DepartmentName.Contains("جاتوه")
                             || d.DepartmentName.Contains("شرقي")
                             || d.DepartmentName.Contains("اسبشيال")
                             || d.DepartmentName.Contains("مخبوزات")
                             || d.DepartmentName.Contains("كرواسون"))
                    .Where(d => !d.DepartmentName.Contains("إدارة")
                             && !d.DepartmentName.Contains("العليا")
                             && !d.DepartmentName.Contains("المالية")
                             && !d.DepartmentName.Contains("الموارد"))
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new { d.Id, d.DepartmentName })
                    .ToListAsync();

                var warehouses = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => (w.WarehouseName.Contains("خامات")
                              || w.WarehouseName.Contains("رئيسي")
                              || w.WarehouseName.Contains("مستودع")
                              || w.WarehouseName.Contains("مركزي"))
                             && !w.WarehouseName.Contains("صالة")
                             && !w.WarehouseName.Contains("قسم")
                             && !w.WarehouseName.Contains("إدارة"))
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new { w.Id, w.WarehouseName })
                    .ToListAsync();

                if (!warehouses.Any())
                {
                    warehouses = await _context.Warehouses
                        .AsNoTracking()
                        .Where(w => !w.WarehouseName.Contains("صالة") && !w.WarehouseName.Contains("قسم"))
                        .OrderBy(w => w.WarehouseName)
                        .Select(w => new { w.Id, w.WarehouseName })
                        .ToListAsync();
                }

                var rawMaterials = await _context.Items
                    .AsNoTracking()
                    .Where(i => i.Active == true && (i.ItemType == "خامات" || i.ItemType == "منتج وسيط" || i.ItemType == "نصف مصنع" || i.ItemType == "عجين"))
                    .OrderBy(i => i.ItemName)
                    .Select(i => new { i.Id, i.ItemCode, i.ItemName, i.ItemType, i.Unit })
                    .ToListAsync();

                return Ok(new { departments, warehouses, rawMaterials });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية: {ex.Message}");
            }
        }

        // ========================================================
        // 2. إدارة طلبات صرف الخامات
        // ========================================================
        [HttpGet]
        public async Task<ActionResult<List<RequisitionDetailDto>>> GetAll([FromQuery] string? status)
        {
            try
            {
                var result = await _service.GetAllRequisitionsAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استرجاع طلبات الصرف: {ex.Message}");
            }
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<RequisitionDetailDto>> GetById(long id)
        {
            try
            {
                var result = await _service.GetRequisitionByIdAsync(id);
                if (result == null) return NotFound("طلب الصرف غير موجود.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<RequisitionDetailDto>> Create([FromBody] CreateRequisitionDto dto)
        {
            try
            {
                if (dto == null || dto.Lines == null || !dto.Lines.Any())
                    return BadRequest("يجب إدخال خامة واحدة على الأقل في طلب الصرف.");

                var result = await _service.CreateRequisitionAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"فشل إنشاء طلب الصرف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("fefo-suggestions")]
        public async Task<ActionResult<List<FefoBatchSuggestionDto>>> GetFefo([FromQuery] long itemId, [FromQuery] long warehouseId)
        {
            try
            {
                var suggestions = await _service.GetFefoSuggestionsAsync(itemId, warehouseId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب اقتراحات FEFO: {ex.Message}");
            }
        }

        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchRequisitionDto dto)
        {
            try
            {
                var success = await _service.DispatchRequisitionAsync(dto);
                if (!success) return BadRequest("فشل تنفيذ صرف الخامات وتخصيص اللوطات.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء صرف الخامات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("receive-line")]
        public async Task<IActionResult> ReceiveLine([FromBody] ReceiveLineItemDto dto)
        {
            try
            {
                var success = await _service.ReceiveLineItemAsync(dto);
                if (!success) return BadRequest("فشل تحديث حالة استلام السطر.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسجيل استلام السطر: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("resolve-discrepancy")]
        public async Task<IActionResult> ResolveDiscrepancy([FromBody] ResolveDiscrepancyDto dto)
        {
            try
            {
                var success = await _service.ResolveDiscrepancyAsync(dto);
                if (!success) return BadRequest("تعذر العثور على سطر النزاع المطلوب تسويته.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسوية الخلاف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 3. التقرير الرقابي الشامل لدورة حياة الخامات وأرصدة الصالات التراكمية
        // ========================================================
        [HttpGet("audit-lifecycle-report")]
        public async Task<IActionResult> GetAuditLifecycleReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] long? departmentId,
            [FromQuery] long? itemId,
            [FromQuery] string? status)
        {
            try
            {
                // جلب كافة طلبات الصرف لتأسيس كشف الحساب التراكمي التاريخي الصحيح
                var allRequisitions = await _service.GetAllRequisitionsAsync(null);
                if (allRequisitions == null || !allRequisitions.Any())
                    return Ok(new List<RequisitionLifecycleAuditDto>());

                var allWarehouses = await _context.Warehouses.AsNoTracking().ToListAsync();

                // تفكيك الطلبات إلى حركات أسطر تفصيلية
                var flatList = new List<RequisitionLifecycleAuditDto>();

                foreach (var req in allRequisitions)
                {
                    var floorStore = allWarehouses.FirstOrDefault(w =>
                        !string.IsNullOrEmpty(req.DepartmentName) &&
                        (w.WarehouseName.Contains(req.DepartmentName) ||
                         (req.DepartmentName.Contains("شرقي") && w.WarehouseName.Contains("شرقي")) ||
                         (req.DepartmentName.Contains("جاتوه") && w.WarehouseName.Contains("جاتوه")) ||
                         (req.DepartmentName.Contains("تورت") && w.WarehouseName.Contains("تورت")) ||
                         (req.DepartmentName.Contains("اسبشيال") && w.WarehouseName.Contains("اسبشيال"))));

                    string floorStoreName = floorStore?.WarehouseName ?? $"مخزن صالة {req.DepartmentName}";
                    string centralStoreName = !string.IsNullOrEmpty(req.FromWarehouseName) ? req.FromWarehouseName : "المستودع الرئيسي";

                    foreach (var line in req.Lines)
                    {
                        // احتساب الكمية المعتمدة الفعلية وهالك النزاع
                        decimal finalApproved = 0m;
                        decimal scrapLoss = 0m;
                        string disputeStatus = "مطابق بدون نزاع ✓";

                        if (line.LineStatus == "ACCEPTED")
                        {
                            finalApproved = line.ReceivedQty > 0 ? line.ReceivedQty : line.DispatchedQty;
                            scrapLoss = line.DispatchedQty > finalApproved ? (line.DispatchedQty - finalApproved) : 0m;
                            disputeStatus = scrapLoss > 0 ? "تم فض النزاع واعتماد التسوية ⚖️" : "مطابق بدون نزاع ✓";
                        }
                        else if (line.LineStatus == "DISPUTED")
                        {
                            finalApproved = line.ReceivedQty;
                            scrapLoss = line.DispatchedQty > line.ReceivedQty ? (line.DispatchedQty - line.ReceivedQty) : 0m;
                            disputeStatus = !string.IsNullOrWhiteSpace(line.WarehouseNotes)
                                ? "تم فض النزاع واعتماد التسوية ⚖️"
                                : "نزاع كميات معلق ⚠️";
                        }
                        else if (req.Status == "DISPATCHED" || line.LineStatus == "PENDING")
                        {
                            finalApproved = 0m;
                            scrapLoss = 0m;
                            disputeStatus = "شحنة واردة قيد الفحص بالصالة";
                        }
                        else
                        {
                            finalApproved = 0m;
                            scrapLoss = 0m;
                            disputeStatus = "بانتظار الصرف من المستودع";
                        }

                        flatList.Add(new RequisitionLifecycleAuditDto
                        {
                            RequisitionId = req.Id,
                            RequisitionNumber = req.RequisitionNumber,
                            RequestDate = req.CreatedAt,
                            DepartmentId = req.DepartmentId,
                            DepartmentName = req.DepartmentName,
                            RequestedBy = !string.IsNullOrWhiteSpace(req.RequestedBy) ? req.RequestedBy : "مشرف الصالة",
                            OverallStatus = req.Status,

                            ItemId = line.ItemId,
                            ItemCode = line.ItemCode,
                            ItemName = line.ItemName,
                            Unit = line.Unit,
                            RequestedQty = line.RequestedQty,

                            DispatchedBy = !string.IsNullOrWhiteSpace(req.DispatchedBy) ? req.DispatchedBy : "أمين المخزن",
                            DispatchedAt = req.DispatchedAt,
                            FromWarehouseName = centralStoreName,
                            DispatchedQty = line.DispatchedQty,
                            BatchNumber = !string.IsNullOrWhiteSpace(line.BatchNumber) ? line.BatchNumber : "-",
                            ExpiryDate = line.ExpiryDate,

                            ReceivedQty = line.ReceivedQty,
                            LineStatus = line.LineStatus,
                            DisputeStatusTitle = disputeStatus,
                            DiscrepancyReason = line.DiscrepancyReason,

                            FinalApprovedQty = finalApproved,
                            ScrapOrLossQty = scrapLoss,
                            ResolutionNotes = line.WarehouseNotes,

                            FloorStoreName = floorStoreName
                        });
                    }
                }

                // ========================================================
                // محرك احتساب الأرصدة التراكمية التاريخية للصالات (Ledger Calculation)
                // ========================================================
                var processedRows = new List<RequisitionLifecycleAuditDto>();

                // تجميع الحركات بحسب (الصالة + الصنف) وحساب الأرصدة بالترتيب الزمني الصاعد
                var groups = flatList.GroupBy(x => new { x.DepartmentId, x.ItemId });

                foreach (var grp in groups)
                {
                    decimal runningFloorBalance = 0m;
                    var sortedChronological = grp.OrderBy(x => x.RequestDate).ThenBy(x => x.RequisitionId).ToList();

                    foreach (var row in sortedChronological)
                    {
                        row.FloorStockBefore = Math.Round(runningFloorBalance, 2);

                        // عند اعتماد واستلام السطر تُضاف الكمية المعتمدة فوراً لرصيد الصالة
                        if (row.FinalApprovedQty > 0)
                        {
                            runningFloorBalance += row.FinalApprovedQty;
                        }

                        row.FloorStockAfter = Math.Round(runningFloorBalance, 2);
                    }

                    // الرصيد الفعلي الحالي التراكمي في مخزن الصالة لكافة الحركات التابعة لهذا الصنف
                    decimal currentTotalBalance = Math.Round(runningFloorBalance, 2);
                    foreach (var row in sortedChronological)
                    {
                        row.CurrentFloorStock = currentTotalBalance;
                        processedRows.Add(row);
                    }
                }

                // تطبيق الفلاتر المطلوبة من شاشة المستخدم
                var filteredResult = processedRows.AsEnumerable();

                if (startDate.HasValue)
                    filteredResult = filteredResult.Where(r => r.RequestDate.Date >= startDate.Value.Date);

                if (endDate.HasValue)
                    filteredResult = filteredResult.Where(r => r.RequestDate.Date <= endDate.Value.Date);

                if (departmentId.HasValue && departmentId.Value > 0)
                    filteredResult = filteredResult.Where(r => r.DepartmentId == departmentId.Value);

                if (itemId.HasValue && itemId.Value > 0)
                    filteredResult = filteredResult.Where(r => r.ItemId == itemId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                    filteredResult = filteredResult.Where(r => r.OverallStatus.Equals(status, StringComparison.OrdinalIgnoreCase));

                // الترتيب التنازلي للعرض (الأحدث أولاً)
                var finalReport = filteredResult.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.RequisitionId).ToList();

                return Ok(finalReport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج التقرير الرقابي: {ex.Message}");
            }
        }
    }

    // ==========================================
    // DTO التقرير الرقابي الشامل لدورة حياة الخامات
    // ==========================================
    public class RequisitionLifecycleAuditDto
    {
        public long RequisitionId { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;

        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQty { get; set; }

        public string? DispatchedBy { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public decimal DispatchedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        public decimal ReceivedQty { get; set; }
        public string LineStatus { get; set; } = string.Empty;

        // تفاصيل مسار النزاع وقرار التسوية
        public string DisputeStatusTitle { get; set; } = "مطابق بدون نزاع ✓";
        public string? DiscrepancyReason { get; set; }
        public decimal FinalApprovedQty { get; set; }
        public decimal ScrapOrLossQty { get; set; }
        public string? ResolutionNotes { get; set; }

        // أرصدة الصالة التراكمية
        public string FloorStoreName { get; set; } = string.Empty;
        public decimal FloorStockBefore { get; set; }
        public decimal FloorStockAfter { get; set; }
        public decimal CurrentFloorStock { get; set; }
    }
}