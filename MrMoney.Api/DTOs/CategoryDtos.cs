using System.ComponentModel.DataAnnotations;

namespace MrMoney.Api.DTOs
{
    public class CreateCategoryRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Icon { get; set; } = "faReceipt";

        [Required]
        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color must be a valid hex color.")]
        public string Color { get; set; } = "#6B7280";

        [Required]
        [RegularExpression("^(income|expense|all)$", ErrorMessage = "Type must be income, expense, or all.")]
        public string Type { get; set; } = "all";
    }

    public class UpdateCategoryRequest
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        public string? Color { get; set; }

        [RegularExpression("^(income|expense|all)$")]
        public string? Type { get; set; }
    }

    public class CategoryResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
