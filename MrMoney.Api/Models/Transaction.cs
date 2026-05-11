namespace MrMoney.Api.Models
{
    /// <summary>
    /// Represents a financial transaction (Income, Expense, Transfer).
    /// Maps to the "Transactions" sheet in Google Sheets.
    /// Columns: Id | UserId | AccountId | Name | Category | Amount | Type | Description | Status | Date | CreatedAt
    /// </summary>
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // income | expense | transfer
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
