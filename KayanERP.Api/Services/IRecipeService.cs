using System.Collections.Generic;
using System.Threading.Tasks;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public interface IRecipeService
    {
        Task<List<RecipeDto>> GetAllRecipesAsync();
        Task<RecipeDto?> GetRecipeByIdAsync(long id);
        Task<bool> SaveRecipeAsync(SaveRecipeDto dto);
        Task<int> BulkImportRecipesAsync(List<SaveRecipeDto> dtoList);
        Task<bool> DeleteRecipeAsync(long id);
    }
}