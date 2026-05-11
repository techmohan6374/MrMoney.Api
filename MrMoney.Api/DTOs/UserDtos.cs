using System.ComponentModel.DataAnnotations;

namespace MrMoney.Api.DTOs
{
    public class UpdateUserProfileRequest
    {
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>Direct URL to the avatar (Google Drive public link or Google OAuth picture URL).</summary>
        [StringLength(2048)]
        public string? Picture { get; set; }

        [RegularExpression("^(INR|USD|EUR|GBP|JPY)$", ErrorMessage = "Currency must be INR, USD, EUR, GBP, or JPY.")]
        public string? Currency { get; set; }

        public bool? EmailNotifications { get; set; }

        [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be light or dark.")]
        public string? Theme { get; set; }
    }

    public class UserProfileResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public bool EmailNotifications { get; set; }
        public string Theme { get; set; } = "light";
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
    }

    public class UploadAvatarResponse
    {
        public string PictureUrl { get; set; } = string.Empty;
    }
}
