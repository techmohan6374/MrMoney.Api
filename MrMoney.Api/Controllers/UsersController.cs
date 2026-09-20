using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UsersController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _userRepo.GetAllAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe([FromQuery] string? id, [FromQuery] string? email)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst(ClaimTypes.Email)?.Value
                          ?? id
                          ?? email;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User identification is required." });
                }

                var profile = await _userRepo.GetByIdAsync(userId)
                           ?? await _userRepo.GetByEmailAsync(userId);

                if (profile == null)
                {
                    return NotFound(new { message = "User profile not found." });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserProfile request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst(ClaimTypes.Email)?.Value
                          ?? request.Id
                          ?? request.Email;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User identification is required." });
                }

                var profile = await _userRepo.GetByIdAsync(userId)
                           ?? await _userRepo.GetByEmailAsync(userId);

                if (profile == null && !string.IsNullOrEmpty(request.Email))
                {
                    profile = await _userRepo.GetByEmailAsync(request.Email);
                }

                if (profile == null)
                {
                    return NotFound(new { message = "User profile not found." });
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    profile.Name = request.Name.Trim();
                }
                if (!string.IsNullOrWhiteSpace(request.Picture))
                {
                    profile.Picture = request.Picture.Trim();
                }

                await _userRepo.UpdateAsync(profile);

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _userRepo.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
