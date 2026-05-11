using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllByUserAsync(string userId);
        Task<Category?> GetByIdAsync(string userId, string categoryId);
        Task<Category> CreateAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task DeleteAsync(string userId, string categoryId);
        Task SeedDefaultCategoriesAsync(string userId);
    }
}
