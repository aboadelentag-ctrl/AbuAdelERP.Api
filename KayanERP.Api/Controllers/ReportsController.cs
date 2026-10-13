using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("efficiency")]
        public async Task<IActionResult> GetEfficiencyReport([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] long departmentId = 0)
        {
            try
            {
                var startDt = startDate.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
                var endDt = endDate.ToDateTime(TimeOnly.MaxValue).ToUniversalTime();

                // 1. حساب إجمالي ساعات العمل الفعلية المسجلة من جدول يوميات العمال
                var totalActualHours = await _context.ProductivityLogs
                    .Where(p => p.WorkDate >= startDt && p.WorkDate <= endDt)
                    .SumAsync(p => (decimal?)p.HoursWorked) ?? 0m;

                // 2. الاستعلام من تفاصيل أوامر التشغيل وربطها بالأمر الرئيسي
                var details = await _context.ProductionOrderDetails
                    .Include(d => d.ProductionOrder)
                    .Where(d => d.ProductionOrder != null
                             && d.ProductionOrder.OrderDate >= startDate
                             && d.ProductionOrder.OrderDate <= endDate)
                    .ToListAsync();

                // 3. تجميع الأداء حسب القسم التشغيلي الفعلي
                var departmentGroups = details
                    .Select(d =>
                    {
                        var order = d.ProductionOrder;
                        var notes = order?.Notes ?? "";
                        string deptName = "قسم التشغيل العام";

                        if (notes.Contains("[") && notes.Contains("]"))
                        {
                            int start = notes.IndexOf('[') + 1;
                            int len = notes.IndexOf(']') - start;
                            if (len > 0)
                            {
                                deptName = notes.Substring(start, len);
                            }
                        }

                        // التحقق مما إذا كان أمر التشغيل مقفلاً فعلياً
                        bool isCompleted = order != null && (
                            order.Status == "مكتمل" ||
                            order.Status == "COMPLETED" ||
                            order.Status == "مغلق"
                        );

                        decimal planned = Convert.ToDecimal(d.PlannedQuantity);
                        decimal produced = Convert.ToDecimal(d.ProducedQuantity);

                        // لا يُحسب المخطط للكفاءة ولا يُحسب الهالك إلا للأوامر المنجزة
                        decimal plannedForKpi = isCompleted ? planned : 0m;
                        decimal waste = isCompleted && planned > produced ? (planned - produced) : 0m;

                        return new
                        {
                            DepartmentName = deptName,
                            Planned = plannedForKpi,
                            Produced = produced,
                            Waste = waste
                        };
                    })
                    .GroupBy(x => x.DepartmentName)
                    .Select(g =>
                    {
                        var totalPlan = g.Sum(x => x.Planned);
                        var totalProd = g.Sum(x => x.Produced);
                        var totalWst = g.Sum(x => x.Waste);

                        return new DepartmentEfficiencyDto
                        {
                            DepartmentName = g.Key,
                            TotalPlanned = totalPlan,
                            TotalProduced = totalProd,
                            TotalWaste = totalWst,
                            EfficiencyPercentage = totalPlan > 0
                                ? Math.Round((totalProd / totalPlan) * 100, 1)
                                : 0m
                        };
                    })
                    .Where(g => g.TotalPlanned > 0 || g.TotalProduced > 0)
                    .ToList();

                // 4. احتساب عدد الأوامر المكتملة
                var completedOrdersCount = details
                    .Select(d => d.ProductionOrder)
                    .Where(o => o != null && (
                        o.Status == "مكتمل" ||
                        o.Status == "COMPLETED" ||
                        o.Status == "مغلق"
                    ))
                    .Select(o => o!.Id)
                    .Distinct()
                    .Count();

                var totalPlannedAll = departmentGroups.Sum(d => d.TotalPlanned);
                var totalProducedAll = departmentGroups.Sum(d => d.TotalProduced);

                var summary = new KpiSummaryDto
                {
                    TotalCompletedOrders = completedOrdersCount,
                    TotalWaste = departmentGroups.Sum(d => d.TotalWaste),
                    TotalWorkingHours = totalActualHours,
                    AverageEfficiency = totalPlannedAll > 0
                        ? Math.Round((totalProducedAll / totalPlannedAll) * 100, 1)
                        : 0m
                };

                return Ok(new ReportResultDto
                {
                    Summary = summary,
                    DepartmentEfficiencies = departmentGroups
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ في استخراج مؤشرات الأداء: {ex.Message}");
            }
        }
    }

    public class ReportResultDto
    {
        public KpiSummaryDto Summary { get; set; } = new();
        public List<DepartmentEfficiencyDto> DepartmentEfficiencies { get; set; } = new();
    }

    public class KpiSummaryDto
    {
        public int TotalCompletedOrders { get; set; }
        public decimal AverageEfficiency { get; set; }
        public decimal TotalWaste { get; set; }
        public decimal TotalWorkingHours { get; set; }
    }

    public class DepartmentEfficiencyDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalPlanned { get; set; }
        public decimal TotalProduced { get; set; }
        public decimal EfficiencyPercentage { get; set; }
        public decimal TotalWaste { get; set; }
    }
}