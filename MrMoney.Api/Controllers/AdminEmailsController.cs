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
        private readonly MrMoney.Api.Infrastructure.EmailService _emailService;

        public AdminEmailsController(IAdminEmailRepository repo, MrMoney.Api.Infrastructure.EmailService emailService)
        {
            _repo = repo;
            _emailService = emailService;
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> TestMail()
        {
            try
            {
                var testOrder = new Models.Order
                {
                    Id = "TEST_ORDER_123",
                    PlacedAt = DateTime.UtcNow,
                    Name = "Test Client",
                    Email = "test@example.com",
                    Phone = "1234567890",
                    Address = "123 Test St",
                    City = "Test City",
                    State = "Test State",
                    Pincode = "123456",
                    Subtotal = 1000,
                    Gst = 180,
                    Total = 1180,
                    Items = new System.Collections.Generic.List<Models.OrderItem>
                    {
                        new Models.OrderItem { Id = "P1", Name = "Test Product", Price = 1000, Qty = 1, Image = "" }
                    }
                };

                await _emailService.SendNewOrderEmailAsync(testOrder);
                return Ok(new { message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Failed to send email.", 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message 
                });
            }
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
