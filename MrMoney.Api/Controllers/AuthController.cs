using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MrMoney.Api.Models;
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

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[]
                    {
                        _configuration["GoogleAuth:ClientId"]
                    }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name,payload.Name),
                    new Claim(ClaimTypes.Email,payload.Email)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(7),
                    signingCredentials:
                    credentials);

                var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    jwtToken,
                    user = new
                    {
                        payload.Name,
                        payload.Email,
                        payload.Picture
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        message = ex.Message
                    });
            }
        }
    }
}
