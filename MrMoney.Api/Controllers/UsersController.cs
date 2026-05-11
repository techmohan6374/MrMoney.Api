using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.DTOs;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Services;
using System.Security.Claims;

namespace MrMoney.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly GoogleDriveClient _driveClient;

        // Allowed image MIME types
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };

        public UsersController(IUserService userService, GoogleDriveClient driveClient)
        {
            _userService = userService;
            _driveClient = driveClient;
        }

        /// <summary>Returns the authenticated user's profile.</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profile = await _userService.GetProfileAsync(GetUserId());
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Updates the authenticated user's profile (name, currency, theme, notifications).</summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var profile = await _userService.UpdateProfileAsync(GetUserId(), request);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a new avatar image to Google Drive and updates the user's picture URL.
        /// Accepts multipart/form-data with a single file field named "file".
        /// Max size: 2 MB.
        /// </summary>
        [HttpPost("upload-avatar")]
        [RequestSizeLimit(2 * 1024 * 1024)] // 2 MB hard limit
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Invalid file type. Allowed: JPG, PNG, GIF, WebP." });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum size is 2 MB." });

            try
            {
                var userId = GetUserId();

                // Get current profile to find the old Drive file ID (if any)
                var currentProfile = await _userService.GetProfileAsync(userId);
                var oldFileId      = GoogleDriveClient.ExtractFileId(currentProfile.Picture);

                // Upload to Google Drive — replaces old file
                string pictureUrl;
                using (var stream = file.OpenReadStream())
                {
                    var safeFileName = $"avatar_{userId}_{Path.GetRandomFileName()}{Path.GetExtension(file.FileName)}";
                    pictureUrl = await _driveClient.UploadAvatarAsync(
                        stream,
                        safeFileName,
                        file.ContentType,
                        oldFileId);
                }

                // Save the new Drive URL to the user profile
                var updated = await _userService.UpdateProfileAsync(userId, new UpdateUserProfileRequest
                {
                    Picture = pictureUrl
                });

                return Ok(new UploadAvatarResponse { PictureUrl = updated.Picture });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
