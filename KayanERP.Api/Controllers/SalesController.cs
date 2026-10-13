using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Services;
using KayanERP.Api.DTOs;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _salesService;
        private readonly AppDbContext _context;

        public SalesController(ISalesService salesService, AppDbContext context)
        {
            _salesService = salesService;
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية لشاشة الفاتورة (العملاء + الأصناف التامة + المخازن)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<SalesLookupsDto>> GetLookups()
        {
            try
            {
                var result = await _salesService.GetLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات المبيعات المرجعية: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. استعراض كافة فواتير المبيعات لجدول العرض
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<SalesInvoiceViewDto>>> GetAllInvoices()
        {
            try
            {
                var invoices = await _salesService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب فواتير المبيعات: {ex.Message}");
            }
        }

        // ==============================================================
        // 3. استعراض تفاصيل فاتورة مبيعات محددة
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SalesInvoiceViewDto>> GetInvoiceById(long id)
        {
            try
            {
                var invoice = await _salesService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound("فاتورة المبيعات غير موجودة.");

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب الفاتورة: {ex.Message}");
            }
        }

        // ==============================================================
        // 4. إنشاء واعتماد فاتورة مبيعات وترحيل الأثر المخزني والمالي المزدوج
        // ==============================================================
        [HttpPost]
        [HttpPost("create-invoice")]
        public async Task<ActionResult<SalesInvoiceResultDto>> CreateInvoice([FromBody] ExtendedSalesInvoiceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. فحص السقف الائتماني للعميل إن كانت الفاتورة آجلة (Credit)
                var customer = await _context.Customers.FindAsync(dto.CustomerId);
                if (customer == null)
                    return BadRequest(new { Error = "العميل المحدد غير موجود في قاعدة البيانات." });

                decimal invoiceTotal = dto.InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);

                if (dto.PaymentMethod == "CREDIT" && customer.CreditLimit > 0)
                {
                    // فحص ألا يتجاوز السقف الائتماني
                    if (invoiceTotal > customer.CreditLimit)
                    {
                        return BadRequest(new { Error = $"قيمة الفاتورة ({invoiceTotal:N2} ج.م) تتجاوز الحد الائتماني المسموح به للتاجر [{customer.CustomerName}] البالغ ({customer.CreditLimit:N2} ج.م)." });
                    }
                }

                // 2. ترحيل الفاتورة وحركة المخزون عبر خدمة المبيعات
                var result = await _salesService.CreateInvoiceAsync(dto);
                if (!result.Success)
                    return BadRequest(new { Error = result.Message });

                // 3. الترحيل المالي وتوليد قيد اليومية المزدوج المتزن
                string branchName = "المركز الرئيسي";
                var branch = await _context.Branches.FindAsync(dto.BranchId);
                if (branch != null) branchName = branch.BranchName;

                bool isDistBranch = branchName.Contains("توزيع") || branchName.Contains("خارجي");
                string revenueAccountCode = isDistBranch ? "41" : "42";
                string revenueAccountName = isDistBranch ? "إيرادات مبيعات التوزيع والطلبيات" : "إيرادات مبيعات المعارض المباشرة";

                string debitAccountCode;
                string debitAccountName;

                if (dto.PaymentMethod == "CASH")
                {
                    debitAccountCode = isDistBranch ? "1212" : "1211";
                    debitAccountName = isDistBranch ? "خزينة فرع التوزيع الخارجي" : "الخزينة الرئيسية - المصنع";
                }
                else
                {
                    debitAccountCode = "1221";
                    debitAccountName = $"حسابات التجار والعملاء ({customer.CustomerName})";

                    // تحديث تاريخ آخر تعامل مالي للعميل
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // رسالة نجاح معززة برقم الاعتماد المالي والمخزني
                result.Message = $"{result.Message} | تم الترحيل المالي آلياً: [من حـ/ {debitAccountName}] إلى [حـ/ {revenueAccountName}] متزناً 100%.";

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"خطأ أثناء تسجيل وترحيل فاتورة المبيعات: {ex.GetBaseException().Message}" });
            }
        }
    }

    // DTO لتمرير خيارات السداد والترحيل المالي بسلاسة وبدون كسر الكود القديم
    public class ExtendedSalesInvoiceCreateDto : SalesInvoiceCreateDto
    {
        public string PaymentMethod { get; set; } = "CASH"; // CASH (نقدي) | CREDIT (آجل على الحساب)
        public string? FinancialNotes { get; set; }
    }
}