using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Controllers
{
    public class GoogleLoginDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    public class AdminLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepo;

        private const string AdminEmail = "stargraphix2010@gmail.com";
        private const string AdminPassword = "StarGraphix@ManoVeera123";

        public AuthController(IConfiguration configuration, IUserRepository userRepo)
        {
            _configuration = configuration;
            _userRepo = userRepo;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new { message = "Google User ID and Email are required." });
                }

                // Upsert user profile
                var existingUser = await _userRepo.GetByIdAsync(dto.Id);
                UserProfile user;

                if (existingUser == null)
                {
                    user = new UserProfile
                    {
                        Id = dto.Id,
                        Email = dto.Email,
                        Name = dto.Name,
                        Picture = dto.Picture,
                        Role = "user",
                        Provider = "google",
                        JoinedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    await _userRepo.CreateAsync(user);
                }
                else
                {
                    existingUser.LastLoginAt = DateTime.UtcNow;
                    existingUser.Picture = dto.Picture;
                    existingUser.Name = dto.Name;
                    await _userRepo.UpdateAsync(existingUser);
                    user = existingUser;
                }

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    jwtToken = token,
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Picture,
                        user.Role,
                        user.Provider
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginDto dto)
        {
            try
            {
                if (dto.Email != AdminEmail || dto.Password != AdminPassword)
                {
                    return Unauthorized(new { message = "Invalid admin credentials." });
                }

                // Retrieve or create static Admin profile
                var adminId = "admin_001";
                var adminUser = await _userRepo.GetByIdAsync(adminId);

                if (adminUser == null)
                {
                    adminUser = new UserProfile
                    {
                        Id = adminId,
                        Email = AdminEmail,
                        Name = "StarGraphix Admin",
                        Picture = null,
                        Role = "admin",
                        Provider = "static",
                        JoinedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    await _userRepo.CreateAsync(adminUser);
                }
                else
                {
                    adminUser.LastLoginAt = DateTime.UtcNow;
                    await _userRepo.UpdateAsync(adminUser);
                }

                var token = GenerateJwtToken(adminUser);

                return Ok(new
                {
                    jwtToken = token,
                    user = new
                    {
                        adminUser.Id,
                        adminUser.Name,
                        adminUser.Email,
                        adminUser.Picture,
                        adminUser.Role,
                        adminUser.Provider
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GenerateJwtToken(UserProfile user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name,           user.Name),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:             _configuration["Jwt:Issuer"],
                audience:           _configuration["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
