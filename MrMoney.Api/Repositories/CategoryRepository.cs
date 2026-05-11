using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    /// <summary>
    /// Reads and writes Category records to the "Categories" sheet.
    /// Sheet columns (1-based): Id | UserId | Name | Icon | Color | Type | CreatedAt
    /// </summary>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly GoogleSheetsClient _sheets;

        // Default categories seeded for every new user (mirrors the frontend defaults)
        private static readonly List<(string Name, string Icon, string Color, string Type)> DefaultCategories = new()
        {
            ("Food",     "faMugHot",      "#F97316", "expense"),
            ("Shopping", "faShoppingCart","#A855F7", "expense"),
            ("Travel",   "faTicket",      "#EC4899", "expense"),
            ("Bills",    "faReceipt",     "#EF4444", "expense"),
            ("Salary",   "faBriefcase",   "#10B981", "income"),
            ("Invest",   "faChartLine",   "#3B82F6", "income"),
            ("Others",   "faPlus",        "#6B7280", "all")
        };

        public CategoryRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<List<Category>> GetAllByUserAsync(string userId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.CategoriesSheet);
            var result = new List<Category>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 1) == userId)
                    result.Add(MapRowToCategory(row));
            }

            return result.OrderBy(c => c.CreatedAt).ToList();
        }

        public async Task<Category?> GetByIdAsync(string userId, string categoryId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.CategoriesSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 0) == categoryId && GetCell(row, 1) == userId)
                    return MapRowToCategory(row);
            }
            return null;
        }

        // ── Write ────────────────────────────────────────────────────────────

        public async Task<Category> CreateAsync(Category category)
        {
            await _sheets.AppendRowAsync(GoogleSheetsClient.CategoriesSheet, MapCategoryToRow(category));
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.CategoriesSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == category.Id && GetCell(rows[i], 1) == category.UserId)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.CategoriesSheet, i + 1, MapCategoryToRow(category));
                    return category;
                }
            }
            throw new KeyNotFoundException($"Category '{category.Id}' not found.");
        }

        public async Task DeleteAsync(string userId, string categoryId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.CategoriesSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == categoryId && GetCell(rows[i], 1) == userId)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.CategoriesSheet, i + 1);
                    return;
                }
            }
        }

        /// <summary>
        /// Seeds the 7 default categories for a brand-new user.
        /// Called once after first login.
        /// </summary>
        public async Task SeedDefaultCategoriesAsync(string userId)
        {
            var existing = await GetAllByUserAsync(userId);
            if (existing.Count > 0) return; // Already seeded

            var now = DateTime.UtcNow;
            foreach (var (name, icon, color, type) in DefaultCategories)
            {
                var category = new Category
                {
                    Id        = Guid.NewGuid().ToString(),
                    UserId    = userId,
                    Name      = name,
                    Icon      = icon,
                    Color     = color,
                    Type      = type,
                    CreatedAt = now
                };
                await CreateAsync(category);
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapCategoryToRow(Category c) => new List<object>
        {
            c.Id,
            c.UserId,
            c.Name,
            c.Icon,
            c.Color,
            c.Type,
            c.CreatedAt.ToString("o")
        };

        private static Category MapRowToCategory(IList<object> row) => new()
        {
            Id        = GetCell(row, 0),
            UserId    = GetCell(row, 1),
            Name      = GetCell(row, 2),
            Icon      = GetCell(row, 3).IfEmpty("faReceipt"),
            Color     = GetCell(row, 4).IfEmpty("#6B7280"),
            Type      = GetCell(row, 5).IfEmpty("all"),
            CreatedAt = DateTime.TryParse(GetCell(row, 6), out var ca) ? ca : DateTime.UtcNow
        };

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }
}
