using MrMoney.Api.DTOs;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepo;

        public CategoryService(ICategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<List<CategoryResponse>> GetAllAsync(string userId)
        {
            var categories = await _categoryRepo.GetAllByUserAsync(userId);
            return categories.Select(MapToResponse).ToList();
        }

        public async Task<CategoryResponse> GetByIdAsync(string userId, string categoryId)
        {
            var category = await _categoryRepo.GetByIdAsync(userId, categoryId)
                ?? throw new KeyNotFoundException($"Category '{categoryId}' not found.");
            return MapToResponse(category);
        }

        public async Task<CategoryResponse> CreateAsync(string userId, CreateCategoryRequest request)
        {
            // Prevent duplicate category names for the same user
            var existing = await _categoryRepo.GetAllByUserAsync(userId);
            if (existing.Any(c => c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

            var category = new Category
            {
                Id        = Guid.NewGuid().ToString(),
                UserId    = userId,
                Name      = request.Name.Trim(),
                Icon      = request.Icon,
                Color     = request.Color,
                Type      = request.Type,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _categoryRepo.CreateAsync(category);
            return MapToResponse(created);
        }

        public async Task<CategoryResponse> UpdateAsync(string userId, string categoryId, UpdateCategoryRequest request)
        {
            var category = await _categoryRepo.GetByIdAsync(userId, categoryId)
                ?? throw new KeyNotFoundException($"Category '{categoryId}' not found.");

            if (request.Name  != null) category.Name  = request.Name.Trim();
            if (request.Icon  != null) category.Icon  = request.Icon;
            if (request.Color != null) category.Color = request.Color;
            if (request.Type  != null) category.Type  = request.Type;

            var updated = await _categoryRepo.UpdateAsync(category);
            return MapToResponse(updated);
        }

        public async Task DeleteAsync(string userId, string categoryId)
        {
            _ = await _categoryRepo.GetByIdAsync(userId, categoryId)
                ?? throw new KeyNotFoundException($"Category '{categoryId}' not found.");

            await _categoryRepo.DeleteAsync(userId, categoryId);
        }

        private static CategoryResponse MapToResponse(Category c) => new()
        {
            Id        = c.Id,
            Name      = c.Name,
            Icon      = c.Icon,
            Color     = c.Color,
            Type      = c.Type,
            CreatedAt = c.CreatedAt
        };
    }
}
