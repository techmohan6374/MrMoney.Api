using System;

namespace MrMoney.Api.Models
{
    /// <summary>
    /// Represents a user profile stored after Google OAuth login or admin credentials.
    /// Columns: Id | Email | Name | Picture | Role | Provider | JoinedAt | LastLoginAt
    /// </summary>
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
        public string Role { get; set; } = "user"; // "user" or "admin"
        public string Provider { get; set; } = "google"; // "google" or "static"
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}
