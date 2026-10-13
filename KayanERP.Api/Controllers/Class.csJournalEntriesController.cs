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
    public class JournalEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // سجل القيود المحاسبية التراكمي
        private static readonly List<JournalEntryViewDto> _journalStore = new()
        {
            new JournalEntryViewDto
            {
                Id = 1,
                EntryNumber = "JV-2026-0001",
                EntryDate = DateTime.Today.AddDays(-2),
                Description = "قيد استحقاق مبيعات يومية - فرع التوزيع الخارجي (طلب مرن)",
                ReferenceType = "SALES_INVOICE",
                ReferenceNumber = "INV-DIST-101",
                BranchName = "فرع المبيعات الخارجية والتوزيع",
                IsPosted = true,
                Lines = new List<JournalEntryLineViewDto>
                {
                    new() { Id = 1, AccountCode = "1221", AccountName = "حسابات كبار التجار (محلات الدسوقي)", Debit = 42500, Credit = 0, Memo = "استحقاق بضاعة يومية مسلّمة" },
                    new() { Id = 2, AccountCode = "41", AccountName = "إيرادات مبيعات التوزيع والطلبيات", Debit = 0, Credit = 42500, Memo = "إيراد مبيعات حلويات شرقية" }
                }
            },
            new JournalEntryViewDto
            {
                Id = 2,
                EntryNumber = "JV-2026-0002",
                EntryDate = DateTime.Today.AddDays(-1),
                Description = "قيد تحصيل نقدي بخزينة التوزيع من تاجر",
                ReferenceType = "RECEIPT_VOUCHER",
                ReferenceNumber = "RV-2026-001",
                BranchName = "فرع المبيعات الخارجية والتوزيع",
                IsPosted = true,
                Lines = new List<JournalEntryLineViewDto>
                {
                    new() { Id = 3, AccountCode = "1212", AccountName = "خزينة فرع التوزيع الخارجي", Debit = 25000, Credit = 0, Memo = "إيداع نقدي بالخزينة" },
                    new() { Id = 4, AccountCode = "1221", AccountName = "حسابات كبار التجار (محلات الدسوقي)", Debit = 0, Credit = 25000, Memo = "سداد دفعة نقدية وتخفيض مديونية" }
                }
            },
            new JournalEntryViewDto
            {
                Id = 3,
                EntryNumber = "JV-2026-0003",
                EntryDate = DateTime.Today,
                Description = "قيد صرف خامات من المستودع لصالة الشرقي (أمر تشغيل MES)",
                ReferenceType = "MATERIAL_REQUISITION",
                ReferenceNumber = "REQ-045",
                BranchName = "المعرض الرئيسي (المصنع)",
                IsPosted = true,
                Lines = new List<JournalEntryLineViewDto>
                {
                    new() { Id = 5, AccountCode = "1232", AccountName = "إنتاج تحت التشغيل (صالة الشرقي WIP)", Debit = 18200, Credit = 0, Memo = "صرف سمن ودقيق ومكسرات" },
                    new() { Id = 6, AccountCode = "1231", AccountName = "مخزن المواد الخام والتعبئة", Debit = 0, Credit = 18200, Memo = "تخفيض رصيد مخزن الخامات" }
                }
            }
        };

        public JournalEntriesController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. استعراض كافة القيود مع الفلترة
        // ==========================================
        [HttpGet]
        public ActionResult<List<JournalEntryViewDto>> GetAll([FromQuery] string? refType, [FromQuery] string? search)
        {
            var query = _journalStore.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(refType))
                query = query.Where(j => j.ReferenceType.Equals(refType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(j => j.EntryNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || j.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || j.Lines.Any(l => l.AccountName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return Ok(query.OrderByDescending(j => j.Id).ToList());
        }

        // ==========================================
        // 2. إنشاء واعتماد قيد محاسبي جديد
        // ==========================================
        [HttpPost]
        public ActionResult<JournalEntryViewDto> Create([FromBody] JournalEntryViewDto dto)
        {
            if (dto == null || dto.Lines == null || dto.Lines.Count < 2)
                return BadRequest("القيد المحاسبي يجب أن يحتوي على طرفين (مدين ودائن) على الأقل.");

            decimal totalDebit = dto.Lines.Sum(l => l.Debit);
            decimal totalCredit = dto.Lines.Sum(l => l.Credit);

            if (totalDebit != totalCredit || totalDebit <= 0)
                return BadRequest($"القيد غير متزن! إجمالي المدين ({totalDebit:N2}) لا يساوي إجمالي الدائن ({totalCredit:N2}).");

            dto.Id = _journalStore.Any() ? _journalStore.Max(j => j.Id) + 1 : 1;
            dto.EntryNumber = $"JV-{DateTime.Now:yyyyMM}-{dto.Id:D4}";
            dto.EntryDate = dto.EntryDate == default ? DateTime.Today : dto.EntryDate;
            dto.IsPosted = true;

            long lineId = 1;
            foreach (var line in dto.Lines)
            {
                line.Id = lineId++;
            }

            _journalStore.Add(dto);
            return Ok(dto);
        }

        // ==========================================
        // 3. مؤشرات وإجماليات القيود (KPIs)
        // ==========================================
        [HttpGet("summary")]
        public ActionResult<object> GetSummary()
        {
            var totalVolume = _journalStore.SelectMany(j => j.Lines).Sum(l => l.Debit);
            return Ok(new
            {
                entriesCount = _journalStore.Count,
                totalDebitVolume = totalVolume,
                autoEntriesCount = _journalStore.Count(j => j.ReferenceType != "MANUAL"),
                manualEntriesCount = _journalStore.Count(j => j.ReferenceType == "MANUAL")
            });
        }
    }

    public class JournalEntryViewDto
    {
        public long Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; } = DateTime.Today;
        public string Description { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = "MANUAL"; // SALES_INVOICE, RECEIPT_VOUCHER, MATERIAL_REQUISITION, MANUAL
        public string? ReferenceNumber { get; set; }
        public string BranchName { get; set; } = "المركز الرئيسي";
        public bool IsPosted { get; set; } = true;

        public decimal TotalDebit => Lines?.Sum(l => l.Debit) ?? 0;
        public decimal TotalCredit => Lines?.Sum(l => l.Credit) ?? 0;

        public List<JournalEntryLineViewDto> Lines { get; set; } = new();
    }

    public class JournalEntryLineViewDto
    {
        public long Id { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public string? CostCenterName { get; set; }
        public string? Memo { get; set; }
    }
}