using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesController(ISalesService salesService)
        {
            _salesService = salesService;
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
        // 4. إنشاء واعتماد فاتورة مبيعات وترحيل الأثر المخزني
        // ==============================================================
        [HttpPost]
        [HttpPost("create-invoice")]
        public async Task<ActionResult<SalesInvoiceResultDto>> CreateInvoice([FromBody] SalesInvoiceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _salesService.CreateInvoiceAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"خطأ أثناء تسجيل فاتورة المبيعات: {ex.GetBaseException().Message}" });
            }
        }
    }
}