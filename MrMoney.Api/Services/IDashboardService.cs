using MrMoney.Api.DTOs;

namespace MrMoney.Api.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync(string userId);
        Task<ReportSummaryResponse> GetReportAsync(string userId, string period, string? accountId, string? txType);
    }
}
