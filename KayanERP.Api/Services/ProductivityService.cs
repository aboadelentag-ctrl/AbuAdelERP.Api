using KayanERP.Shared.DTOs;
using KayanERP.Api.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Services
{
    public class ProductivityService : IProductivityService
    {
        private readonly AppDbContext _context;

        public ProductivityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> AddDepartmentAsync(DepartmentCreateDto dto)
        {
            var dept = new Department
            {
                CompanyId = 6,
                DepartmentCode = dto.DepartmentCode,
                DepartmentName = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Departments.AddAsync(dept);
            await _context.SaveChangesAsync();

            return new { Message = "تم تسجيل القسم بنجاح", DepartmentId = dept.Id };
        }

        public async Task<object> AddEmployeeAsync(EmployeeCreateDto dto)
        {
            var employee = new Employee
            {
                CompanyId = 6,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name,
                EmployeeCode = dto.EmployeeCode,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            return new { Message = "تم تسجيل الموظف بنجاح", EmployeeId = employee.Id };
        }

        public async Task<object> GetEmployeeProductivityAsync(long employeeId)
        {
            // جلب الموظف مع بيانات القسم المرتبط به في استعلام واحد لتحسين الأداء
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(w => w.Id == employeeId);

            if (employee == null)
                return new { Message = "عفواً، لم يتم العثور على هذا الموظف." };

            string deptName = employee.Department != null ? employee.Department.DepartmentName : "قسم غير محدد";

            // جلب إنتاجية الموظف من جدول التفاصيل
            var employeeDetails = await _context.ProductionOrderDetails
                .Include(d => d.ProductionOrder)
                .Where(d => d.EmployeeId == employeeId && (d.Status == "COMPLETED" || d.Status == "مكتمل"))
                .ToListAsync();

            decimal totalProduced = employeeDetails.Sum(d => d.ProducedQuantity);
            decimal totalHours = employeeDetails.Sum(d => d.WorkingHours);
            decimal totalWaste = employeeDetails.Sum(d => d.WasteQuantity);
            int totalOrdersCount = employeeDetails.Select(d => d.ProductionOrderId).Distinct().Count();

            decimal hourlyRate = totalHours > 0 ? Math.Round(totalProduced / totalHours, 2) : 0;
            decimal wastePercentage = totalProduced > 0 ? Math.Round((totalWaste / totalProduced) * 100, 2) : 0;

            string evaluation = "ممتاز - كفاءة عالية";
            if (wastePercentage > 5)
                evaluation = "يحتاج مراجعة - نسبة الهالك مرتفعة وتتخطى 5%";
            else if (hourlyRate > 0 && hourlyRate < 10)
                evaluation = "بطيء - جودة مقبولة ولكن يحتاج إلى تحسين السرعة";

            return new
            {
                EmployeeName = employee.Name,
                EmployeeCode = employee.EmployeeCode,
                Department = deptName,
                ProductivityReport = new
                {
                    TotalOrdersExecuted = totalOrdersCount,
                    TotalQuantityProduced = totalProduced,
                    TotalWorkingHours = totalHours,
                    ProductionPerHour = hourlyRate,
                    TotalWaste = totalWaste,
                    WastePercentage = $"{wastePercentage}%",
                    Evaluation = totalProduced > 0 ? evaluation : "لا توجد إنتاجية مسجلة حتى الآن"
                }
            };
        }

        public async Task<byte[]> ExportEmployeeProductivityToExcelAsync(long employeeId)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Abu Adel ERP");

            // الاستعلام المدمج لجلب بيانات الموظف مع القسم
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(w => w.Id == employeeId);

            if (employee == null)
                throw new Exception("الموظف غير موجود");

            var employeeDetails = await _context.ProductionOrderDetails
                .Include(d => d.ProductionOrder)
                .Where(d => d.EmployeeId == employeeId && (d.Status == "COMPLETED" || d.Status == "مكتمل"))
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("تقرير الإنتاجية");
                worksheet.View.RightToLeft = true;

                // تنسيق الترويسة الرئيسية
                worksheet.Cells["A1:F1"].Merge = true;
                worksheet.Cells["A1"].Value = $"تقرير كفاءة وإنتاجية الموظف - مؤسسة حلوانى أبو عادل";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 78, 120));
                worksheet.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.White);

                // بيانات الموظف
                worksheet.Cells["A3"].Value = "اسم الموظف:";
                worksheet.Cells["B3"].Value = employee.Name;
                worksheet.Cells["C3"].Value = "كود الموظف:";
                worksheet.Cells["D3"].Value = employee.EmployeeCode;
                worksheet.Cells["E3"].Value = "القسم:";
                worksheet.Cells["F3"].Value = employee.Department?.DepartmentName ?? "غير محدد";

                // رؤوس الأعمدة
                worksheet.Cells["A5"].Value = "رقم أمر الإنتاج";
                worksheet.Cells["B5"].Value = "الكمية المنتجة (كجم)";
                worksheet.Cells["C5"].Value = "ساعات العمل";
                worksheet.Cells["D5"].Value = "معدل الساعات (كجم/ساعة)";
                worksheet.Cells["E5"].Value = "الهالك (كجم)";
                worksheet.Cells["F5"].Value = "تاريخ الإنتاج";

                using (var range = worksheet.Cells["A5:F5"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(217, 226, 236));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // تعبئة البيانات
                int row = 6;
                foreach (var detail in employeeDetails)
                {
                    decimal rate = detail.WorkingHours > 0 ? Math.Round(detail.ProducedQuantity / detail.WorkingHours, 2) : 0;

                    worksheet.Cells[row, 1].Value = detail.ProductionOrder?.OrderNumber ?? "N/A";
                    worksheet.Cells[row, 2].Value = detail.ProducedQuantity;
                    worksheet.Cells[row, 3].Value = detail.WorkingHours;
                    worksheet.Cells[row, 4].Value = rate;
                    worksheet.Cells[row, 5].Value = detail.WasteQuantity;
                    worksheet.Cells[row, 6].Value = detail.ProductionOrder?.OrderDate.ToString("yyyy-MM-dd") ?? "";

                    row++;
                }

                // صف الإجماليات
                worksheet.Cells[row, 1].Value = "الإجمالي";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Formula = $"SUM(B6:B{row - 1})";
                worksheet.Cells[row, 3].Formula = $"SUM(C6:C{row - 1})";
                worksheet.Cells[row, 5].Formula = $"SUM(E6:E{row - 1})";

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await package.GetAsByteArrayAsync();
            }
        }
    }
}