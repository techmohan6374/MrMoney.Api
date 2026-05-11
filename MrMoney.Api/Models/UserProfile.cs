namespace MrMoney.Api.Models
{
    /// <summary>
    /// Represents a user profile stored after Google OAuth login.
    /// Maps to the "Users" sheet in Google Sheets.
    /// Columns: Id | Email | Name | Picture | Currency | EmailNotifications | Theme | CreatedAt | LastLoginAt
    /// </summary>
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty; // Google Subject (sub) claim
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;  // Google URL or base64 data URL
        public string Currency { get; set; } = "INR";
        public bool EmailNotifications { get; set; } = true;
        public string Theme { get; set; } = "light";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}
