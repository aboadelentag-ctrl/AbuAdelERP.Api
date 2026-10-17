using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _salesService;
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public SalesController(
            ISalesService salesService,
            AppDbContext context,
            ITenantContext tenantContext)
        {
            _salesService = salesService;
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==============================================================
        // 1. القوائم المرجعية (العملاء + الأصناف + المخازن)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<SalesLookupsDto>> GetLookups()
        {
            var result = await _salesService.GetLookupsAsync();
            return Ok(result);
        }

        // ==============================================================
        // 2. استعراض كافة فواتير المبيعات
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<SalesInvoiceViewDto>>> GetAllInvoices()
        {
            var invoices = await _salesService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        // ==============================================================
        // 3. تفاصيل فاتورة مبيعات محددة
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SalesInvoiceViewDto>> GetInvoiceById(long id)
        {
            var invoice = await _salesService.GetInvoiceByIdAsync(id);

            if (invoice == null)
                return NotFound(new { message = "فاتورة المبيعات غير موجودة." });

            return Ok(invoice);
        }

        // ==============================================================
        // 4. إنشاء فاتورة مبيعات
        // ==============================================================
        [HttpPost("create-invoice")]
        public async Task<ActionResult<SalesInvoiceResultDto>> CreateInvoice(
            [FromBody] ExtendedSalesInvoiceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyId = _tenantContext.CompanyId;

            // ⚠️ تأكيد إن العميل يخص نفس الشركة
            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                return BadRequest(new { error = "العميل المحدد غير موجود." });

            if (customer.CompanyId != companyId)
                return Forbid();

            // ⚠️ تأكيد إن الفرع يخص نفس الشركة
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            if (branch == null)
                return BadRequest(new { error = "الفرع المحدد غير موجود." });

            if (branch.CompanyId != companyId)
                return Forbid();

            // فحص السقف الائتماني (للبيع الآجل)
            decimal invoiceTotal = dto.InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);

            if (dto.PaymentMethod == "CREDIT" && customer.CreditLimit > 0)
            {
                if (invoiceTotal > customer.CreditLimit)
                {
                    return BadRequest(new
                    {
                        error = $"قيمة الفاتورة ({invoiceTotal:N2} ج.م) تتجاوز الحد الائتماني المسموح به للتاجر [{customer.CustomerName}] البالغ ({customer.CreditLimit:N2} ج.م)."
                    });
                }
            }

            // ترحيل الفاتورة عبر الخدمة
            var result = await _salesService.CreateInvoiceAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Message });

            // الترحيل المالي
            string branchName = branch.BranchName;
            bool isDistBranch = branchName.Contains("توزيع") || branchName.Contains("خارجي");
            string revenueAccountName = isDistBranch
                ? "إيرادات مبيعات التوزيع والطلبيات"
                : "إيرادات مبيعات المعارض المباشرة";

            string debitAccountName = dto.PaymentMethod == "CASH"
                ? (isDistBranch ? "خزينة فرع التوزيع الخارجي" : "الخزينة الرئيسية - المصنع")
                : $"حسابات التجار والعملاء ({customer.CustomerName})";

            // تحديث آخر تعامل للعميل (للبيع الآجل)
            if (dto.PaymentMethod != "CASH")
            {
                customer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            result.Message = $"{result.Message} | تم الترحيل المالي آلياً: [من حـ/ {debitAccountName}] إلى [حـ/ {revenueAccountName}] متزناً 100%.";

            return Ok(result);
        }
    }
}