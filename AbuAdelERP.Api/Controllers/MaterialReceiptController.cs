using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialReceiptController : ControllerBase
    {
        private readonly IMaterialReceiptService _receiptService;

        public MaterialReceiptController(IMaterialReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

        // ==============================================================
        // 1. القوائم المرجعية (الموردين، المستودعات)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<MaterialReceiptLookupsDto>> GetLookups()
        {
            try
            {
                var result = await _receiptService.GetLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب الفواتير وأوامر الشراء المعتمدة الجاهزة للاستلام
        // ==============================================================
        [HttpGet("pending-invoices")]
        public async Task<ActionResult<List<PendingInvoiceDto>>> GetPendingInvoices([FromQuery] long? supplierId = null)
        {
            try
            {
                var result = await _receiptService.GetPendingInvoicesAsync(supplierId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الفواتير المعتمدة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تأكيد الاستلام الميداني وفحص الجودة وتسكين المخزن
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult<MaterialReceiptResultDto>> PostMaterialReceipt([FromBody] MaterialReceiptPostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _receiptService.ProcessMaterialReceiptAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ الاستلام المخزني: {ex.GetBaseException().Message}");
            }
        }
    }
}