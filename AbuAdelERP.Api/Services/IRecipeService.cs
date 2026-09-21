using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Services
{
    public interface IRecipeService
    {
        Task<List<RecipeDto>> GetAllRecipesAsync();
        Task<RecipeDto?> GetRecipeByIdAsync(long id);
        Task<bool> SaveRecipeAsync(SaveRecipeDto dto);
        Task<bool> DeleteRecipeAsync(long id);
    }
}