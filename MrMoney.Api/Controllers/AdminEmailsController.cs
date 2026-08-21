using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Controllers
{
    public class AdminEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminEmailsController : ControllerBase
    {
        private readonly IAdminEmailRepository _repo;

        public AdminEmailsController(IAdminEmailRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();

                var list = await _repo.GetAllAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AdminEmailDto dto)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();

                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new { message = "Email is required." });
                }

                await _repo.AddAsync(dto.Email);
                return Ok(new { message = "Email added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> Delete(string email)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();

                await _repo.DeleteAsync(email);
                return Ok(new { message = "Email deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
