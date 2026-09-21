using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs
{
    // كلاس إنشاء الوصفة (يعتمد تلقائياً على RecipeLineDto المعرّف في RecipeDtos.cs)
    public class RecipeCreateDto
    {
        [Required]
        public string RecipeCode { get; set; } = string.Empty;

        [Required]
        public string RecipeName { get; set; } = string.Empty;

        public long ProductItemId { get; set; } // رقم المنتج التام المستهدف

        public decimal BatchQuantity { get; set; }

        public string BatchUnit { get; set; } = string.Empty;

        // قائمة الخامات المطلوبة للخلطة
        public List<RecipeLineDto> Lines { get; set; } = new List<RecipeLineDto>();
    }
}