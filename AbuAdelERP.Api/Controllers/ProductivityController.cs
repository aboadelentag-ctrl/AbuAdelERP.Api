using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductivityController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IProductivityService _productivityService;

        public ProductivityController(AppDbContext context, IProductivityService productivityService)
        {
            _context = context;
            _productivityService = productivityService;
        }

        // ==========================================
        // دوال القوائم المنسدلة لتسهيل الاختيار في الواجهة
        // ==========================================

        [HttpGet("lookup/employees")]
        public async Task<IActionResult> GetEmployeesLookup()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Active)
                .Select(e => new EmployeeLookupDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "غير محدد"
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("lookup/orders")]
        public async Task<IActionResult> GetOrdersLookup()
        {
            var orders = await _context.ProductionOrders
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Select(o => new OrderLookupDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Notes = o.Notes,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ==========================================
        // عمليات تسجيل الإنتاجية اليومية والمؤشرات
        // ==========================================

        [HttpPost("log-work")]
        public async Task<IActionResult> LogWork([FromBody] ProductivityLogRequestDto request)
        {
            try
            {
                string formattedNotes = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"[الوحدة: {request.Unit} | الهالك: {request.WastedQuantity}]"
                    : $"[الوحدة: {request.Unit} | الهالك: {request.WastedQuantity}] - {request.Notes}";

                var log = new ProductivityLog
                {
                    ProductionOrderId = request.ProductionOrderId,
                    EmployeeId = request.EmployeeId,
                    WorkDate = request.WorkDate.ToUniversalTime(),
                    HoursWorked = request.HoursWorked,
                    ProducedQuantity = request.ProducedQuantity,
                    Notes = formattedNotes
                };

                _context.ProductivityLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم تسجيل بيانات الإنتاجية بنجاح." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"خطأ أثناء التسجيل: {ex.Message}" });
            }
        }

        [HttpGet("department-kpi")]
        public async Task<IActionResult> GetDepartmentKPIs([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var startUtc = startDate.Date.ToUniversalTime();
            var endUtc = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var kpis = await _context.ProductivityLogs
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Where(p => p.WorkDate >= startUtc && p.WorkDate <= endUtc)
                .GroupBy(p => p.Employee != null && p.Employee.Department != null ? p.Employee.Department.DepartmentName : "غير محدد")
                .Select(g => new DepartmentKpiDto
                {
                    DepartmentName = g.Key,
                    TotalHours = g.Sum(x => x.HoursWorked),
                    TotalProduced = g.Sum(x => x.ProducedQuantity),
                    ProductivityPerHour = g.Sum(x => x.HoursWorked) > 0
                        ? Math.Round(g.Sum(x => x.ProducedQuantity) / g.Sum(x => x.HoursWorked), 3)
                        : 0
                })
                .ToListAsync();

            return Ok(kpis);
        }

        // ==========================================
        // تقارير إنتاجية الموظف وتصدير الإكسيل
        // ==========================================

        [HttpGet("employee/{employeeId}/report")]
        public async Task<IActionResult> GetEmployeeProductivityReport(long employeeId)
        {
            var result = await _productivityService.GetEmployeeProductivityAsync(employeeId);
            return Ok(result);
        }

        [HttpGet("employee/{employeeId}/export-excel")]
        public async Task<IActionResult> ExportEmployeeProductivityExcel(long employeeId)
        {
            try
            {
                var fileContent = await _productivityService.ExportEmployeeProductivityToExcelAsync(employeeId);
                string fileName = $"ProductivityReport_Employee_{employeeId}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"حدث خطأ أثناء استخراج التقرير: {ex.Message}" });
            }
        }
    }

    // ==========================================
    // DTOs (Data Transfer Objects)
    // ==========================================

    public class ProductivityLogRequestDto
    {
        public long ProductionOrderId { get; set; }
        public long EmployeeId { get; set; }
        public DateTime WorkDate { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal ProducedQuantity { get; set; }
        public decimal WastedQuantity { get; set; } = 0;
        public string Unit { get; set; } = "كجم";
        public string Notes { get; set; } = string.Empty;
    }

    public class DepartmentKpiDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal TotalProduced { get; set; }
        public decimal ProductivityPerHour { get; set; }
    }

    public class EmployeeLookupDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class OrderLookupDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateOnly OrderDate { get; set; }
    }
}