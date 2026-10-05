using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
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

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductionOrderListDto>> GetOrder(long id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return NotFound("أمر التشغيل غير موجود.");
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateProductionOrderDto dto)
        {
            try
            {
                var success = await _orderService.CreateOrderAsync(dto);
                if (!success) return BadRequest("تعذر إصدار أمر التشغيل. يرجى مراجعة البيانات.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في إصدار الأمر: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}