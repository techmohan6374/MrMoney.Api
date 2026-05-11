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
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _txService;

        public TransactionsController(ITransactionService txService)
        {
            _txService = txService;
        }

        /// <summary>
        /// Returns a paged, filtered list of transactions for the authenticated user.
        /// Supports filtering by type, accountId, category, search text, and date range.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TransactionFilterRequest filter)
        {
            var result = await _txService.GetAllAsync(GetUserId(), filter);
            return Ok(result);
        }

        /// <summary>Returns a single transaction by ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var tx = await _txService.GetByIdAsync(GetUserId(), id);
                return Ok(tx);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Creates a new income or expense transaction and updates the linked account balance.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tx = await _txService.CreateAsync(GetUserId(), request);
                return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Updates an existing transaction.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tx = await _txService.UpdateAsync(GetUserId(), id, request);
                return Ok(tx);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Deletes a transaction.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _txService.DeleteAsync(GetUserId(), id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Transfers funds between two accounts.
        /// Creates a Transfer Out (expense) and Transfer In (income) transaction pair.
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var (from, to) = await _txService.TransferAsync(GetUserId(), request);
                return Ok(new { transferOut = from, transferIn = to });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
