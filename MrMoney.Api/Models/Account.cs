namespace MrMoney.Api.Models
{
    /// <summary>
    /// Represents a financial account (Bank, Cash, Savings, Credit).
    /// Maps to the "Accounts" sheet in Google Sheets.
    /// Columns: Id | UserId | Name | HolderName | Balance | Type | Color | IsDefault | CreatedAt
    /// </summary>
    public class Account
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Type { get; set; } = "Savings"; // Card | Cash | Savings | Bank | Credit
        public string Color { get; set; } = "#10B981";
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
