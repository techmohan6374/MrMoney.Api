namespace MrMoney.Api.Models
{
    /// <summary>
    /// Represents a transaction category (Food, Shopping, Salary, etc.).
    /// Maps to the "Categories" sheet in Google Sheets.
    /// Columns: Id | UserId | Name | Icon | Color | Type | CreatedAt
    /// </summary>
    public class Category
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "faReceipt";
        public string Color { get; set; } = "#6B7280";
        public string Type { get; set; } = "all"; // income | expense | all
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
