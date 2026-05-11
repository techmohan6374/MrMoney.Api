using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.DTOs;
using MrMoney.Api.Services;
using System.Security.Claims;

namespace MrMoney.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>Returns all accounts for the authenticated user.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId   = GetUserId();
            var accounts = await _accountService.GetAllAsync(userId);
            return Ok(accounts);
        }

        /// <summary>Returns a single account by ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var account = await _accountService.GetByIdAsync(GetUserId(), id);
                return Ok(account);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Creates a new account.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var account = await _accountService.CreateAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        /// <summary>Updates an existing account.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var account = await _accountService.UpdateAsync(GetUserId(), id, request);
                return Ok(account);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Deletes an account.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _accountService.DeleteAsync(GetUserId(), id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
