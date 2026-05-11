using System.ComponentModel.DataAnnotations;

namespace MrMoney.Api.DTOs
{
    public class CreateAccountRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string HolderName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Balance must be non-negative.")]
        public decimal Balance { get; set; }

        [Required]
        [RegularExpression("^(Card|Cash|Savings|Bank|Credit)$", ErrorMessage = "Type must be Card, Cash, Savings, Bank, or Credit.")]
        public string Type { get; set; } = "Savings";

        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color must be a valid hex color.")]
        public string Color { get; set; } = "#10B981";

        public bool IsDefault { get; set; }
    }

    public class UpdateAccountRequest
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        [StringLength(100, MinimumLength = 1)]
        public string? HolderName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Balance { get; set; }

        [RegularExpression("^(Card|Cash|Savings|Bank|Credit)$")]
        public string? Type { get; set; }

        [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        public string? Color { get; set; }

        public bool? IsDefault { get; set; }
    }

    public class AccountResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
