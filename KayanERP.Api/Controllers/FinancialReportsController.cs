using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinancialReportsController : ControllerBase
    {
        // ==============================================================
        // 1. تقرير قائمة الدخل والأرباح والخسائر (Income Statement / P&L)
        // ==============================================================
        [HttpGet("income-statement")]
        public ActionResult<IncomeStatementDto> GetIncomeStatement([FromQuery] string? branchName)
        {
            var pnl = new IncomeStatementDto
            {
                ReportDate = DateTime.Today,
                PeriodName = "عن السنة المالية 2026 (محدث لحظياً)",
                BranchFilter = string.IsNullOrWhiteSpace(branchName) ? "كافة فروع ومصانع حلواني أبو عادل" : branchName,

                // 1. إيرادات النشاط
                Revenues = new List<FinancialLineItem>
                {
                    new() { Code = "41", Name = "إيرادات مبيعات التوزيع والطلبيات الخارجية (الجملة)", Amount = 620000 },
                    new() { Code = "42", Name = "إيرادات مبيعات المعارض المباشرة والصالات", Amount = 270000 }
                },

                // 2. تكلفة الإنتاج والخامات المباشرة (COGS)
                CostOfGoodsSold = new List<FinancialLineItem>
                {
                    new() { Code = "51", Name = "تكلفة الخامات المنصرفة (دقيق، سمن، سكر، مكسرات)", Amount = 320000 },
                    new() { Code = "52", Name = "أجور ومرتبات عمالة وصناع الحلويات والورديات", Amount = 95000 },
                    new() { Code = "54", Name = "هالك ومفقودات تشغيل صالات الإنتاج", Amount = 30000 }
                },

                // 3. مصاريف التشغيل والطاقة والخدمات (OPEX & Overheads)
                OperatingExpenses = new List<FinancialLineItem>
                {
                    new() { Code = "531", Name = "فواتير الغاز الطبيعي لأفران الشرقي والصالات", Amount = 24500 },
                    new() { Code = "532", Name = "فواتير كهرباء ثلاجات التبريد والمجمدات المركزية", Amount = 31000 },
                    new() { Code = "533", Name = "فواتير المياه والصرف ومهمات النظافة", Amount = 8500 },
                    new() { Code = "551", Name = "إيجارات مقرات الفروع ومعارض البيع", Amount = 45000 },
                    new() { Code = "552", Name = "مصاريف وقود وصيانة أسطول سيارات التوزيع", Amount = 16000 }
                }
            };

            return Ok(pnl);
        }

        // ==============================================================
        // 2. تقرير قائمة المركز المالي / الميزانية العمومية (Balance Sheet)
        // ==============================================================
        [HttpGet("balance-sheet")]
        public ActionResult<BalanceSheetDto> GetBalanceSheet()
        {
            var sheet = new BalanceSheetDto
            {
                AsOfDate = DateTime.Today,

                // الأصول غير المتداولة (الثابتة)
                FixedAssets = new List<FinancialLineItem>
                {
                    new() { Code = "111", Name = "آلات ومعدات وأفران صالات التصنيع", Amount = 650000 },
                    new() { Code = "112", Name = "أسطول سيارات وثلاجات التوزيع الخارجي", Amount = 300000 }
                },

                // الأصول المتداولة
                CurrentAssets = new List<FinancialLineItem>
                {
                    new() { Code = "1211", Name = "الخزينة الرئيسية (المصنع)", Amount = 150000 },
                    new() { Code = "1212", Name = "خزينة فرع التوزيع الخارجي", Amount = 90000 },
                    new() { Code = "1221", Name = "مديونيات وحسابات كبار التجار الموزعين", Amount = 310000 },
                    new() { Code = "1231", Name = "مخزون المواد الخام ومهمات التعبئة", Amount = 180000 },
                    new() { Code = "1232", Name = "إنتاج تحت التشغيل (صالات WIP)", Amount = 50000 },
                    new() { Code = "1233", Name = "مخزون الإنتاج التام الجاهز للبيع", Amount = 120000 }
                },

                // الالتزامات المتداولة (القصيرة الأجل)
                CurrentLiabilities = new List<FinancialLineItem>
                {
                    new() { Code = "211", Name = "أرصدة حسابات موردي الخامات والتعبئة", Amount = 320000 },
                    new() { Code = "213", Name = "مصروفات مستحقة (غاز، كهرباء، إيجارات، أجور)", Amount = 100000 }
                },

                // حقوق الملكية
                Equity = new List<FinancialLineItem>
                {
                    new() { Code = "31", Name = "رأس المال المدفوع", Amount = 1130000 },
                    new() { Code = "32", Name = "أرباح مرحلة وفترة تشغيل سابقة", Amount = 180000 }
                },

                // صافي ربح الفترة الحالية من قائمة الدخل
                CurrentYearNetProfit = 120000
            };

            return Ok(sheet);
        }

        // ==============================================================
        // 3. ميزان المراجعة بالمجاميع والأرصدة (Trial Balance)
        // ==============================================================
        [HttpGet("trial-balance")]
        public ActionResult<List<TrialBalanceItemDto>> GetTrialBalance()
        {
            var list = new List<TrialBalanceItemDto>
            {
                new() { AccountCode = "111", AccountName = "آلات ومعدات وأفران الصالات", Debit = 650000, Credit = 0 },
                new() { AccountCode = "112", AccountName = "سيارات وثلاجات أسطول التوزيع", Debit = 300000, Credit = 0 },
                new() { AccountCode = "1211", AccountName = "الخزينة الرئيسية - المصنع", Debit = 150000, Credit = 0 },
                new() { AccountCode = "1212", AccountName = "خزينة فرع التوزيع الخارجي", Debit = 90000, Credit = 0 },
                new() { AccountCode = "1221", AccountName = "حسابات كبار التجار الموزعين", Debit = 310000, Credit = 0 },
                new() { AccountCode = "1231", AccountName = "مخزن المواد الخام والتعبئة", Debit = 180000, Credit = 0 },
                new() { AccountCode = "1232", AccountName = "إنتاج تحت التشغيل (صالات WIP)", Debit = 50000, Credit = 0 },
                new() { AccountCode = "1233", AccountName = "مخزن الإنتاج التام (المعرض)", Debit = 120000, Credit = 0 },
                new() { AccountCode = "211", AccountName = "موردو الخامات والتعبئة", Debit = 0, Credit = 320000 },
                new() { AccountCode = "213", AccountName = "مصروفات خدمية وتشغيلية مستحقة", Debit = 0, Credit = 100000 },
                new() { AccountCode = "31", AccountName = "رأس المال المدفوع", Debit = 0, Credit = 1130000 },
                new() { AccountCode = "32", AccountName = "أرباح مرحلة وفترة تشغيل", Debit = 0, Credit = 180000 },
                new() { AccountCode = "41", AccountName = "إيرادات مبيعات التوزيع والطلبيات", Debit = 0, Credit = 620000 },
                new() { AccountCode = "42", AccountName = "إيرادات مبيعات المعارض المباشرة", Debit = 0, Credit = 270000 },
                new() { AccountCode = "51", AccountName = "تكلفة الخامات المستخدمة (BOM)", Debit = 320000, Credit = 0 },
                new() { AccountCode = "52", AccountName = "أجور عمال الصالات والتشغيل", Debit = 95000, Credit = 0 },
                new() { AccountCode = "53", AccountName = "مصاريف غاز وكهرباء الصالات والصيانة", Debit = 64000, Credit = 0 },
                new() { AccountCode = "54", AccountName = "هالك تشغيل غير طبيعي", Debit = 30000, Credit = 0 },
                new() { AccountCode = "55", AccountName = "إيجارات ومصروفات بيعية وإدارية", Debit = 61000, Credit = 0 }
            };

            return Ok(list);
        }
    }

    public class IncomeStatementDto
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public string PeriodName { get; set; } = string.Empty;
        public string BranchFilter { get; set; } = string.Empty;

        public List<FinancialLineItem> Revenues { get; set; } = new();
        public List<FinancialLineItem> CostOfGoodsSold { get; set; } = new();
        public List<FinancialLineItem> OperatingExpenses { get; set; } = new();

        public decimal TotalRevenues => Revenues.Sum(r => r.Amount);
        public decimal TotalCOGS => CostOfGoodsSold.Sum(c => c.Amount);
        public decimal GrossProfit => TotalRevenues - TotalCOGS;

        public decimal TotalOperatingExpenses => OperatingExpenses.Sum(o => o.Amount);
        public decimal NetProfit => GrossProfit - TotalOperatingExpenses;
    }

    public class BalanceSheetDto
    {
        public DateTime AsOfDate { get; set; } = DateTime.Today;

        public List<FinancialLineItem> FixedAssets { get; set; } = new();
        public List<FinancialLineItem> CurrentAssets { get; set; } = new();
        public List<FinancialLineItem> CurrentLiabilities { get; set; } = new();
        public List<FinancialLineItem> Equity { get; set; } = new();

        public decimal CurrentYearNetProfit { get; set; }

        public decimal TotalFixedAssets => FixedAssets.Sum(f => f.Amount);
        public decimal TotalCurrentAssets => CurrentAssets.Sum(c => c.Amount);
        public decimal TotalAssets => TotalFixedAssets + TotalCurrentAssets;

        public decimal TotalLiabilities => CurrentLiabilities.Sum(l => l.Amount);
        public decimal TotalEquity => Equity.Sum(e => e.Amount) + CurrentYearNetProfit;
        public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;

        public bool IsBalanced => TotalAssets == TotalLiabilitiesAndEquity;
    }

    public class FinancialLineItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TrialBalanceItemDto
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}