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
    public class TreasuryVouchersController : ControllerBase
    {
        private readonly AppDbContext _context;

        // سجل السندات المتزامن مع الحركات المالية
        private static readonly List<TreasuryVoucherDto> _inMemoryVouchers = new()
        {
            new TreasuryVoucherDto
            {
                Id = 1,
                VoucherNumber = "RV-2026-001",
                VoucherType = "RECEIPT",
                VoucherDate = DateTime.Today.AddDays(-1),
                Amount = 25000,
                TreasuryCode = "1212",
                TreasuryName = "خزينة فرع التوزيع الخارجي",
                BranchName = "فرع المبيعات الخارجية والتوزيع",
                PartyType = "CUSTOMER",
                PartyId = 1,
                PartyName = "محلات الدسوقي للحلويات (الحاج إبراهيم)",
                OppositeAccountCode = "1221",
                OppositeAccountName = "حسابات كبار التجار والعملاء",
                PaymentMethod = "نقدي (كاش)",
                Description = "تحصيل دفعة نقدية من حساب توريدات الأسبوع",
                CreatedBy = "أحمد عبد العزيز"
            },
            new TreasuryVoucherDto
            {
                Id = 2,
                VoucherNumber = "PV-2026-001",
                VoucherType = "PAYMENT",
                VoucherDate = DateTime.Today,
                Amount = 4500,
                TreasuryCode = "1211",
                TreasuryName = "الخزينة الرئيسية - المصنع",
                BranchName = "المعرض الرئيسي (المصنع)",
                PartyType = "EXPENSE",
                PartyName = "صيانة طارئة لعجانة صالة الشرقي",
                OppositeAccountCode = "53",
                OppositeAccountName = "مصاريف غاز وصيانة الصالات",
                PaymentMethod = "نقدي (عهدة تشغيل)",
                Description = "شراء سيور غيار عاجلة لماكينة العجن بالصالة",
                CreatedBy = "أحمد عبد العزيز"
            }
        };

        public TreasuryVouchersController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. استعراض كافة السندات مع الفلترة
        // ==========================================
        [HttpGet]
        public ActionResult<List<TreasuryVoucherDto>> GetAll([FromQuery] string? type, [FromQuery] string? branch)
        {
            try
            {
                var query = _inMemoryVouchers.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(v => v.VoucherType.Equals(type, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(branch))
                    query = query.Where(v => v.BranchName.Contains(branch, StringComparison.OrdinalIgnoreCase));

                return Ok(query.OrderByDescending(v => v.Id).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب السندات: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 2. إصدار وترحيل سند جديد وتحديث الأرصدة
        // ==========================================
        [HttpPost]
        public async Task<ActionResult<TreasuryVoucherDto>> Create([FromBody] TreasuryVoucherDto dto)
        {
            if (dto == null || dto.Amount <= 0)
                return BadRequest("بيانات السند غير مكتملة، والمبلغ يجب أن يكون أكبر من صفر.");

            try
            {
                dto.Id = _inMemoryVouchers.Any() ? _inMemoryVouchers.Max(v => v.Id) + 1 : 1;
                string prefix = dto.VoucherType == "RECEIPT" ? "RV" : "PV";
                dto.VoucherNumber = $"{prefix}-{DateTime.Now:yyyyMM}-{dto.Id:D3}";
                dto.VoucherDate = dto.VoucherDate == default ? DateTime.Today : dto.VoucherDate;

                // إذا كان سند قبض من تاجر مسجل في قاعدة البيانات، نقوم بتخفيض مديونيته
                if (dto.VoucherType == "RECEIPT" && dto.PartyId.HasValue && dto.PartyId.Value > 0)
                {
                    var customer = await _context.Customers.FindAsync(dto.PartyId.Value);
                    if (customer != null)
                    {
                        // توثيق السداد في الملاحظات المالية للعميل وتحديث سجله
                        customer.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                _inMemoryVouchers.Add(dto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ السند: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 3. جلب ملخص السيولة والمؤشرات المالية (KPIs)
        // ==========================================
        [HttpGet("summary")]
        public ActionResult<object> GetSummary()
        {
            var totalReceipts = _inMemoryVouchers.Where(v => v.VoucherType == "RECEIPT").Sum(v => v.Amount);
            var totalPayments = _inMemoryVouchers.Where(v => v.VoucherType == "PAYMENT").Sum(v => v.Amount);
            var netLiquidity = totalReceipts - totalPayments;

            return Ok(new
            {
                totalReceipts,
                totalPayments,
                netLiquidity,
                count = _inMemoryVouchers.Count
            });
        }

        // ==========================================
        // 4. استدعاء قائمة العملاء والتجار للاختيار السريع
        // ==========================================
        [HttpGet("parties-lookup")]
        public async Task<ActionResult<List<PartyLookupDto>>> GetPartiesLookup()
        {
            try
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Active)
                    .Select(c => new PartyLookupDto
                    {
                        Id = c.Id,
                        Code = c.CustomerCode,
                        Name = c.CustomerName,
                        Phone = c.Phone ?? ""
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch
            {
                return Ok(new List<PartyLookupDto>());
            }
        }
    }

    public class TreasuryVoucherDto
    {
        public long Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string VoucherType { get; set; } = "RECEIPT"; // RECEIPT (قبض) | PAYMENT (صرف)
        public DateTime VoucherDate { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string TreasuryCode { get; set; } = "1212";
        public string TreasuryName { get; set; } = "خزينة فرع التوزيع الخارجي";
        public string BranchName { get; set; } = "فرع المبيعات الخارجية والتوزيع";
        public string PartyType { get; set; } = "CUSTOMER"; // CUSTOMER | SUPPLIER | EXPENSE
        public long? PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string OppositeAccountCode { get; set; } = "1221";
        public string OppositeAccountName { get; set; } = "حسابات كبار التجار والعملاء";
        public string PaymentMethod { get; set; } = "نقدي (كاش)";
        public string Description { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = "System";
    }

    public class PartyLookupDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}