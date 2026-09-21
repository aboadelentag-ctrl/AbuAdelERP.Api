using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly AppDbContext _context;

    public QueryController(AppDbContext context)
    {
        _context = context; // نستخدم قاعدة البيانات مباشرة للسرعة
    }

    [HttpGet("recipe-components/{productId}")]
    public async Task<IActionResult> GetRecipeComponents(long productId)
    {
        // جلب الوصفة مع خطوطها (مكوناتها)
        var recipe = await _context.Recipes
            .Include(r => r.RecipeLines)
            .FirstOrDefaultAsync(r => r.ProductItemId == productId && r.Active);

        if (recipe == null)
            return NotFound(new { Message = $"لا توجد وصفة مسجلة للمنتج رقم {productId}" });

        // تجهيز النتيجة بشكل مبسط وواضح
        var result = new
        {
            Message = "تم جلب مكونات الوصفة بنجاح",
            RecipeId = recipe.Id,
            ProductItemId = recipe.ProductItemId,
            Components = recipe.RecipeLines.Select(line => new
            {
                RawMaterial_ItemId = line.ComponentItemId, // هذا هو الرقم الذي نبحث عنه!
                QuantityPerBatch = line.Quantity
            }).ToList()
        };

        return Ok(result);
    }
}