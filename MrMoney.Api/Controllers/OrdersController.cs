using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Controllers
{
    public class OrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;
        private readonly CloudinaryClient _cloudinaryClient;
        private readonly EmailService _emailService;

        public OrdersController(IOrderRepository orderRepo, CloudinaryClient cloudinaryClient, EmailService emailService)
        {
            _orderRepo = orderRepo;
            _cloudinaryClient = cloudinaryClient;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? userId)
        {
            try
            {
                // Attempt to get user from token claims
                var tokenUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                // Fall back to query param if token is not available
                var effectiveUserId = tokenUserId ?? userId;
                var effectiveRole = role;

                if (string.IsNullOrEmpty(effectiveUserId))
                {
                    // If no user ID is specified, and they are admin, return all
                    if (effectiveRole == "admin")
                    {
                        var allOrders = await _orderRepo.GetAllAsync();
                        return Ok(allOrders);
                    }
                    // Otherwise ask for identification
                    return BadRequest(new { message = "User identification is required." });
                }

                if (effectiveRole == "admin")
                {
                    var allOrders = await _orderRepo.GetAllAsync();
                    return Ok(allOrders);
                }
                else
                {
                    var userOrders = await _orderRepo.GetByUserIdAsync(effectiveUserId);
                    return Ok(userOrders);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _orderRepo.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = "Order not found." });
            }
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Order order)
        {
            try
            {
                order.Status = "Pending Verification";
                order.PlacedAt = DateTime.UtcNow;

                var created = await _orderRepo.CreateAsync(order);

                // Send email notification to admin asynchronously
                try
                {
                    await _emailService.SendNewOrderEmailAsync(created);
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine($"Error sending order confirmation email: {mailEx.Message}");
                }

                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] OrderStatusDto dto)
        {
            try
            {
                var updated = await _orderRepo.UpdateStatusAsync(id, dto.Status);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-screenshot")]
        public async Task<IActionResult> UploadScreenshot(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var url = await _cloudinaryClient.UploadImageAsync(stream, file.FileName);
                return Ok(new { imageUrl = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Screenshot upload failed: {ex.Message}" });
            }
        }
    }
}
