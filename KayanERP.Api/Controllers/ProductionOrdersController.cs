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
    public class ProductionOrdersController : ControllerBase
    {
        private readonly IProductionOrderService _orderService;

        public ProductionOrdersController(IProductionOrderService orderService)
        {
            _orderService = orderService;
        }

        // ==============================================================
        // 1. استعراض كافة أوامر التشغيل والإنتاج
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<ProductionOrderListDto>>> GetOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب أوامر التشغيل: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. جلب تفاصيل أمر تشغيل محدد
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductionOrderListDto>> GetOrder(long id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound("أمر التشغيل غير موجود.");

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب بيانات أمر التشغيل: {ex.Message}");
            }
        }

        // ==============================================================
        // 3. إصدار أمر تشغيل جديد
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateProductionOrderDto dto)
        {
            try
            {
                var success = await _orderService.CreateOrderAsync(dto);
                if (!success)
                    return BadRequest("تعذر إصدار أمر التشغيل. يرجى مراجعة البيانات المدخلة وتوافر الخامات.");

                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في إصدار أمر التشغيل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}