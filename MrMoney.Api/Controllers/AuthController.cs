using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MrMoney.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;
        private readonly ICategoryRepository _categoryRepo;

        public AuthController(
            IConfiguration configuration,
            IUserRepository userRepo,
            ICategoryRepository categoryRepo)
        {
            _configuration = configuration;
            _userRepo      = userRepo;
            _categoryRepo  = categoryRepo;
        }

        /// <summary>
        /// Validates a Google ID token, upserts the user profile in Google Sheets,
        /// seeds default categories on first login, and returns a JWT.
        /// </summary>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                // 1. Validate the Google credential token
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);

                // 2. Upsert user in Google Sheets (Users sheet)
                var existingUser = await _userRepo.GetByIdAsync(payload.Subject);
                UserProfile user;

                if (existingUser == null)
                {
                    // First login — create profile and seed default categories
                    user = new UserProfile
                    {
                        Id          = payload.Subject,
                        Email       = payload.Email,
                        Name        = payload.Name,
                        Picture     = payload.Picture,
                        Currency    = "INR",
                        EmailNotifications = true,
                        Theme       = "light",
                        CreatedAt   = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    await _userRepo.CreateAsync(user);
                    await _categoryRepo.SeedDefaultCategoriesAsync(user.Id);
                }
                else
                {
                    // Returning user — update last login and refresh picture/name from Google
                    existingUser.LastLoginAt = DateTime.UtcNow;
                    existingUser.Picture     = payload.Picture;
                    existingUser.Name        = payload.Name;
                    await _userRepo.UpdateAsync(existingUser);
                    user = existingUser;
                }

                // 3. Issue JWT
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name,           user.Name),
                    new Claim(ClaimTypes.Email,          user.Email)
                };

                var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer:             _configuration["Jwt:Issuer"],
                    audience:           _configuration["Jwt:Audience"],
                    claims:             claims,
                    expires:            DateTime.UtcNow.AddDays(7),
                    signingCredentials: credentials);

                var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    jwtToken,
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Picture,
                        user.Currency,
                        user.Theme
                    }
                });
            }
            catch (InvalidJwtException ex)
            {
                return Unauthorized(new { message = "Invalid Google token.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
