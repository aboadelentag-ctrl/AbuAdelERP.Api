using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _orderService;

        public PurchaseOrdersController(IPurchaseOrderService orderService)
        {
            _orderService = orderService;
        }

        // ==============================================================
        // 1. القوائم المرجعية لأوامر الشراء (موردين، مستودعات، وأصناف)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<PurchaseOrderLookupsDto>> GetLookups()
        {
            try
            {
                var result = await _orderService.GetLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية للشراء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب كافة أوامر الشراء
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<PurchaseOrderDto>>> GetOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب أوامر الشراء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. جلب أمر شراء محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<PurchaseOrderDto>> GetOrder(long id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound($"أمر الشراء رقم {id} غير موجود.");

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام أمر الشراء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. إصدار أمر شراء / طلب احتياج جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult<PurchaseOrderResultDto>> CreateOrder([FromBody] PurchaseOrderCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _orderService.CreateOrderAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ أمر الشراء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. اعتماد الفاتورة وتخصيص الموردين والأسعار
        // ==============================================================
        [HttpPost("approve-invoice")]
        public async Task<IActionResult> ApproveInvoice([FromBody] PoApproveDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _orderService.ApproveInvoiceAsync(dto);
                if (!success)
                    return NotFound("أمر الشراء المطلوب اعتماده غير موجود.");

                return Ok(new { success = true, message = "تم اعتماد أمر الشراء والفاتورة بنجاح!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء اعتماد الفاتورة: {ex.GetBaseException().Message}");
            }
        }
    }
}