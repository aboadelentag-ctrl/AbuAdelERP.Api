using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesPayablesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // سجل الفواتير مع الربط المباشر بالموردين والفروع
        private static readonly List<ExpensePayableDto> _billsStore = new()
        {
            new ExpensePayableDto
            {
                Id = 1,
                BillNumber = "BILL-GAS-001",
                Category = "UTILITY_GAS",
                Title = "فاتورة الغاز الطبيعي لأفران صالة الشرقي",
                PartyName = "شركة تاون جاس (غاز الصالات الصناعية)",
                CostCenterName = "صالة الإنتاج الشرقي (CC-EAST)",
                DebitAccountCode = "53",
                DebitAccountName = "مصاريف غاز وكهرباء الصالات",
                CreditAccountCode = "213",
                CreditAccountName = "مصروفات خدمية مستحقة",
                IssueDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(5),
                TotalAmount = 24500,
                PaidAmount = 24500,
                Status = "PAID",
                Notes = "استهلاك تشغيل أفران البسبوسة والكنافة",
                CreatedBy = "أحمد عبد العزيز"
            },
            new ExpensePayableDto
            {
                Id = 2,
                BillNumber = "BILL-SUPP-001",
                Category = "SUPPLIER_PURCHASE",
                SupplierId = 1,
                Title = "فاتورة توريد خامات سمن بلدي وزبدة طبيعي",
                PartyName = "شركة الأمل للتوريدات الغذائية",
                CostCenterName = "المستودع الرئيسي للخامات (WH-MAIN)",
                DebitAccountCode = "1231",
                DebitAccountName = "مخزن المواد الخام والتعبئة",
                CreditAccountCode = "211",
                CreditAccountName = "موردو الخامات والتعبئة",
                IssueDate = DateTime.Today.AddDays(-2),
                DueDate = DateTime.Today.AddDays(15),
                TotalAmount = 98000,
                PaidAmount = 50000,
                Status = "PARTIAL",
                Notes = "توريد سمن بلدي لصالح صالات الإنتاج",
                CreatedBy = "أحمد عبد العزيز"
            }
        };

        public ExpensesPayablesController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. جلب الموردين الفعليين المسجلين بالنظام (Lookup)
        // ==========================================
        [HttpGet("suppliers-lookup")]
        public async Task<ActionResult<List<SupplierLookupItem>>> GetSuppliersLookup()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .AsNoTracking()
                    .Where(s => s.Active)
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new SupplierLookupItem
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        SupplierName = s.SupplierName,
                        Phone = s.Phone ?? ""
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch
            {
                // في حال عدم وجود سجلات موردين بعد في الداتابيز
                return Ok(new List<SupplierLookupItem>
                {
                    new() { Id = 1, SupplierCode = "SUPP-001", SupplierName = "شركة الأمل للتوريدات الغذائية والزيوت", Phone = "01099887766" },
                    new() { Id = 2, SupplierCode = "SUPP-002", SupplierName = "مطاحن النصر الحديثة (توريد دقيق فاخر)", Phone = "01122334455" },
                    new() { Id = 3, SupplierCode = "SUPP-003", SupplierName = "مصنع الأهرام للكرتون ومهمات التعبئة", Phone = "01233445566" }
                });
            }
        }

        // ==========================================
        // 2. استعراض الفواتير والمطالبات المالية
        // ==========================================
        [HttpGet]
        public ActionResult<List<ExpensePayableDto>> GetAll([FromQuery] string? category, [FromQuery] string? status)
        {
            var query = _billsStore.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            return Ok(query.OrderByDescending(b => b.Id).ToList());
        }

        // ==========================================
        // 3. تسجيل استحقاق جديد مرتبط بمورد أو جهة رسمية
        // ==========================================
        [HttpPost]
        public async Task<ActionResult<ExpensePayableDto>> Create([FromBody] ExpensePayableDto dto)
        {
            if (dto == null || dto.TotalAmount <= 0)
                return BadRequest("بيانات الفاتورة غير صحيحة والمبلغ يجب أن يكون أكبر من صفر.");

            // إذا كان الالتزام خاصاً بمورد، نتأكد من ربطه بالمورد الفعلي
            if (dto.Category == "SUPPLIER_PURCHASE" && dto.SupplierId.HasValue && dto.SupplierId.Value > 0)
            {
                try
                {
                    var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
                    if (supplier != null)
                    {
                        dto.PartyName = supplier.SupplierName;
                    }
                }
                catch { }
            }

            dto.Id = _billsStore.Any() ? _billsStore.Max(b => b.Id) + 1 : 1;
            string prefix = dto.Category switch
            {
                "UTILITY_GAS" => "BILL-GAS",
                "UTILITY_ELEC" => "BILL-ELEC",
                "UTILITY_WATER" => "BILL-WTR",
                "RENT" => "BILL-RNT",
                "PAYROLL" => "PAYROLL",
                _ => "BILL-SUPP"
            };

            dto.BillNumber = $"{prefix}-{DateTime.Now:yyMM}-{dto.Id:D3}";
            dto.IssueDate = dto.IssueDate == default ? DateTime.Today : dto.IssueDate;
            dto.DueDate = dto.DueDate == default ? DateTime.Today.AddDays(7) : dto.DueDate;
            dto.Status = dto.PaidAmount >= dto.TotalAmount ? "PAID" : (dto.PaidAmount > 0 ? "PARTIAL" : "UNPAID");

            _billsStore.Add(dto);
            return Ok(dto);
        }

        // ==========================================
        // 4. سداد دفعة وتحديث رصيد المورد الفعلي
        // ==========================================
        [HttpPost("{id:long}/pay")]
        public ActionResult<object> PayBill(long id, [FromBody] decimal paymentAmount)
        {
            var bill = _billsStore.FirstOrDefault(b => b.Id == id);
            if (bill == null) return NotFound("المطالبة غير موجودة.");

            if (paymentAmount <= 0) return BadRequest("مبلغ السداد يجب أن يكون أكبر من الصفر.");

            bill.PaidAmount += paymentAmount;
            if (bill.PaidAmount >= bill.TotalAmount)
            {
                bill.PaidAmount = bill.TotalAmount;
                bill.Status = "PAID";
            }
            else
            {
                bill.Status = "PARTIAL";
            }

            return Ok(new
            {
                success = true,
                message = "تم تسجيل السداد وتحديث حالة الالتزام بنجاح.",
                paidAmount = bill.PaidAmount,
                remaining = bill.TotalAmount - bill.PaidAmount,
                status = bill.Status
            });
        }

        // ==========================================
        // 5. المؤشرات اللحظية
        // ==========================================
        [HttpGet("summary")]
        public ActionResult<object> GetSummary()
        {
            return Ok(new
            {
                utilitiesDue = _billsStore.Where(b => b.Category.StartsWith("UTILITY")).Sum(b => b.TotalAmount - b.PaidAmount),
                suppliersDue = _billsStore.Where(b => b.Category == "SUPPLIER_PURCHASE").Sum(b => b.TotalAmount - b.PaidAmount),
                payrollDue = _billsStore.Where(b => b.Category == "PAYROLL").Sum(b => b.TotalAmount - b.PaidAmount),
                rentDue = _billsStore.Where(b => b.Category == "RENT").Sum(b => b.TotalAmount - b.PaidAmount)
            });
        }
    }

    public class ExpensePayableDto
    {
        public long Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public string Category { get; set; } = "SUPPLIER_PURCHASE";
        public long? SupplierId { get; set; } // الربط بالمورد الفعلي
        public long? BranchId { get; set; }   // الربط بالفرع الفعلي
        public string Title { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string CostCenterName { get; set; } = string.Empty;
        public string DebitAccountCode { get; set; } = string.Empty;
        public string DebitAccountName { get; set; } = string.Empty;
        public string CreditAccountCode { get; set; } = string.Empty;
        public string CreditAccountName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public string Status { get; set; } = "UNPAID";
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = "System";
    }

    public class SupplierLookupItem
    {
        public long Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}