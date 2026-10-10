using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        // شجرة الحسابات النموذجية الموحدة لمصانع ومنافذ حلواني أبو عادل
        private static readonly List<AccountDto> _defaultAccounts = new()
        {
            // 1. الأصول
            new() { Id = 1, AccountCode = "1", AccountName = "الأصول", Type = 1, Level = 1, IsSubAccount = false, CurrentBalance = 1850000 },
            new() { Id = 2, AccountCode = "11", AccountName = "الأصول الثابتة", Type = 1, ParentId = 1, Level = 2, IsSubAccount = false, CurrentBalance = 950000 },
            new() { Id = 3, AccountCode = "111", AccountName = "آلات ومعدات وأفران الصالات", Type = 1, ParentId = 2, Level = 3, IsSubAccount = true, CurrentBalance = 650000 },
            new() { Id = 4, AccountCode = "112", AccountName = "سيارات وثلاجات أسطول التوزيع", Type = 1, ParentId = 2, Level = 3, IsSubAccount = true, CurrentBalance = 300000 },
            new() { Id = 5, AccountCode = "12", AccountName = "الأصول المتداولة", Type = 1, ParentId = 1, Level = 2, IsSubAccount = false, CurrentBalance = 900000 },
            new() { Id = 6, AccountCode = "121", AccountName = "النقدية والبنوك", Type = 1, ParentId = 5, Level = 3, IsSubAccount = false, CurrentBalance = 240000 },
            new() { Id = 7, AccountCode = "1211", AccountName = "الخزينة الرئيسية - المصنع", Type = 1, ParentId = 6, Level = 4, IsSubAccount = true, CurrentBalance = 150000 },
            new() { Id = 8, AccountCode = "1212", AccountName = "خزينة فرع التوزيع الخارجي", Type = 1, ParentId = 6, Level = 4, IsSubAccount = true, CurrentBalance = 90000 },
            new() { Id = 9, AccountCode = "122", AccountName = "العملاء والذمم المدينة", Type = 1, ParentId = 5, Level = 3, IsSubAccount = false, CurrentBalance = 310000 },
            new() { Id = 10, AccountCode = "1221", AccountName = "حسابات كبار التجار الموزعين (حسابات جارية)", Type = 1, ParentId = 9, Level = 4, IsSubAccount = true, CurrentBalance = 310000 },
            new() { Id = 11, AccountCode = "123", AccountName = "المخزون", Type = 1, ParentId = 5, Level = 3, IsSubAccount = false, CurrentBalance = 350000 },
            new() { Id = 12, AccountCode = "1231", AccountName = "مخزن المواد الخام والتعبئة", Type = 1, ParentId = 11, Level = 4, IsSubAccount = true, CurrentBalance = 180000 },
            new() { Id = 13, AccountCode = "1232", AccountName = "إنتاج تحت التشغيل (صالات الإنتاج WIP)", Type = 1, ParentId = 11, Level = 4, IsSubAccount = true, CurrentBalance = 50000 },
            new() { Id = 14, AccountCode = "1233", AccountName = "مخزن الإنتاج التام (جاهز للتوزيع)", Type = 1, ParentId = 11, Level = 4, IsSubAccount = true, CurrentBalance = 120000 },

            // 2. الالتزامات
            new() { Id = 15, AccountCode = "2", AccountName = "الالتزامات", Type = 2, Level = 1, IsSubAccount = false, CurrentBalance = 420000 },
            new() { Id = 16, AccountCode = "21", AccountName = "الموردون والدائنون", Type = 2, ParentId = 15, Level = 2, IsSubAccount = false, CurrentBalance = 420000 },
            new() { Id = 17, AccountCode = "211", AccountName = "موردو الخامات والتعبئة", Type = 2, ParentId = 16, Level = 3, IsSubAccount = true, CurrentBalance = 420000 },

            // 3. حقوق الملكية
            new() { Id = 18, AccountCode = "3", AccountName = "حقوق الملكية", Type = 3, Level = 1, IsSubAccount = false, CurrentBalance = 1430000 },
            new() { Id = 19, AccountCode = "31", AccountName = "رأس المال المدفوع", Type = 3, ParentId = 18, Level = 2, IsSubAccount = true, CurrentBalance = 1200000 },
            new() { Id = 20, AccountCode = "32", AccountName = "أرباح مرحلة وفترة تشغيل", Type = 3, ParentId = 18, Level = 2, IsSubAccount = true, CurrentBalance = 230000 },

            // 4. الإيرادات
            new() { Id = 21, AccountCode = "4", AccountName = "الإيرادات", Type = 4, Level = 1, IsSubAccount = false, CurrentBalance = 890000 },
            new() { Id = 22, AccountCode = "41", AccountName = "إيرادات مبيعات التوزيع والطلبيات", Type = 4, ParentId = 21, Level = 2, IsSubAccount = true, CurrentBalance = 620000 },
            new() { Id = 23, AccountCode = "42", AccountName = "إيرادات مبيعات المعارض المباشرة", Type = 4, ParentId = 21, Level = 2, IsSubAccount = true, CurrentBalance = 270000 },

            // 5. التكاليف والمصروفات
            new() { Id = 24, AccountCode = "5", AccountName = "التكاليف والمصروفات", Type = 5, Level = 1, IsSubAccount = false, CurrentBalance = 490000 },
            new() { Id = 25, AccountCode = "51", AccountName = "تكلفة الخامات المستخدمة (BOM)", Type = 5, ParentId = 24, Level = 2, IsSubAccount = true, CurrentBalance = 320000 },
            new() { Id = 26, AccountCode = "52", AccountName = "أجور عمال الصالات والتشغيل", Type = 5, ParentId = 24, Level = 2, IsSubAccount = true, CurrentBalance = 95000 },
            new() { Id = 27, AccountCode = "53", AccountName = "مصاريف غاز وكهرباء الصالات والصيانة", Type = 5, ParentId = 24, Level = 2, IsSubAccount = true, CurrentBalance = 45000 },
            new() { Id = 28, AccountCode = "54", AccountName = "هالك تشغيل غير طبيعي", Type = 5, ParentId = 24, Level = 2, IsSubAccount = true, CurrentBalance = 30000 }
        };

        [HttpGet]
        public ActionResult<List<AccountDto>> GetAccounts()
        {
            return Ok(_defaultAccounts.OrderBy(a => a.AccountCode).ToList());
        }

        [HttpPost]
        public ActionResult<AccountDto> CreateAccount([FromBody] AccountDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.AccountName))
                return BadRequest("اسم الحساب مطلوب.");

            dto.Id = _defaultAccounts.Any() ? _defaultAccounts.Max(a => a.Id) + 1 : 1;
            _defaultAccounts.Add(dto);
            return Ok(dto);
        }

        [HttpGet("trial-balance-summary")]
        public ActionResult<object> GetTrialBalanceSummary()
        {
            return Ok(new
            {
                totalAssets = _defaultAccounts.Where(a => a.Type == 1 && a.IsSubAccount).Sum(a => a.CurrentBalance),
                totalLiabilities = _defaultAccounts.Where(a => a.Type == 2 && a.IsSubAccount).Sum(a => a.CurrentBalance),
                totalEquity = _defaultAccounts.Where(a => a.Type == 3 && a.IsSubAccount).Sum(a => a.CurrentBalance),
                totalRevenues = _defaultAccounts.Where(a => a.Type == 4 && a.IsSubAccount).Sum(a => a.CurrentBalance),
                totalExpenses = _defaultAccounts.Where(a => a.Type == 5 && a.IsSubAccount).Sum(a => a.CurrentBalance),
                netIncome = _defaultAccounts.Where(a => a.Type == 4 && a.IsSubAccount).Sum(a => a.CurrentBalance) -
                            _defaultAccounts.Where(a => a.Type == 5 && a.IsSubAccount).Sum(a => a.CurrentBalance)
            });
        }

        public class AccountDto
        {
            public long Id { get; set; }
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public int Type { get; set; } // 1: Asset, 2: Liability, 3: Equity, 4: Revenue, 5: Expense
            public long? ParentId { get; set; }
            public int Level { get; set; } = 1;
            public bool IsSubAccount { get; set; } = true;
            public decimal CurrentBalance { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
}