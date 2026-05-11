using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.Services;
using System.Security.Claims;

namespace MrMoney.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Returns the full dashboard summary:
        /// total income/expense, net balance, today's count, avg daily spend,
        /// top category, largest expense, category breakdown, and 7-day chart data.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var summary = await _dashboardService.GetSummaryAsync(GetUserId());
            return Ok(summary);
        }

        /// <summary>
        /// Returns report analytics for a given period (Daily | Weekly | Monthly | Yearly).
        /// Optionally filtered by accountId and transaction type.
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetReport(
            [FromQuery] string period = "Weekly",
            [FromQuery] string? accountId = null,
            [FromQuery] string? txType = null)
        {
            var report = await _dashboardService.GetReportAsync(GetUserId(), period, accountId, txType);
            return Ok(report);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
