using MrMoney.Api.DTOs;

namespace MrMoney.Api.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryResponse>> GetAllAsync(string userId);
        Task<CategoryResponse> GetByIdAsync(string userId, string categoryId);
        Task<CategoryResponse> CreateAsync(string userId, CreateCategoryRequest request);
        Task<CategoryResponse> UpdateAsync(string userId, string categoryId, UpdateCategoryRequest request);
        Task DeleteAsync(string userId, string categoryId);
    }
}
