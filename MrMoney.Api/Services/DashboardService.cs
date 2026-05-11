using MrMoney.Api.DTOs;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ITransactionRepository _txRepo;
        private readonly IAccountRepository _accountRepo;

        private static readonly string[] ChartColors =
        {
            "#3B82F6", "#F59E0B", "#10B981", "#8B5CF6",
            "#EF4444", "#6B7280", "#EC4899", "#14B8A6"
        };

        public DashboardService(ITransactionRepository txRepo, IAccountRepository accountRepo)
        {
            _txRepo      = txRepo;
            _accountRepo = accountRepo;
        }

        // ── Dashboard Summary ─────────────────────────────────────────────────

        public async Task<DashboardSummaryResponse> GetSummaryAsync(string userId)
        {
            var transactions = await _txRepo.GetAllByUserAsync(userId);
            var accounts     = await _accountRepo.GetAllByUserAsync(userId);

            var totalIncome  = transactions.Where(t => t.Type == "income") .Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Type == "expense").Sum(t => t.Amount);
            var netBalance   = accounts.Sum(a => a.Balance);

            var today        = DateTime.UtcNow.Date;
            var todayCount   = transactions.Count(t => t.Date.Date == today);

            var cutoff       = DateTime.UtcNow.AddDays(-30);
            var recentExpenses = transactions
                .Where(t => t.Type == "expense" && t.Date >= cutoff)
                .Sum(t => t.Amount);
            var avgDailySpend = recentExpenses / 30;

            var topCategory  = GetTopCategory(transactions);
            var largestExp   = GetLargestExpense(transactions);
            var breakdown    = GetCategoryBreakdown(transactions);
            var weekly       = GetWeeklyChartData(transactions);

            return new DashboardSummaryResponse
            {
                TotalIncome             = totalIncome,
                TotalExpense            = totalExpense,
                NetBalance              = netBalance,
                TodayTransactionCount   = todayCount,
                AverageDailySpend       = Math.Round(avgDailySpend, 2),
                TopCategory             = topCategory,
                LargestExpense          = largestExp != null ? MapTxToResponse(largestExp) : null,
                CategoryBreakdown       = breakdown,
                WeeklyChartData         = weekly
            };
        }

        // ── Reports ───────────────────────────────────────────────────────────

        public async Task<ReportSummaryResponse> GetReportAsync(string userId, string period, string? accountId, string? txType)
        {
            var allTransactions = await _txRepo.GetAllByUserAsync(userId);
            var accounts        = await _accountRepo.GetAllByUserAsync(userId);

            // Filter by account
            var transactions = string.IsNullOrWhiteSpace(accountId) || accountId == "All"
                ? allTransactions
                : allTransactions.Where(t => t.AccountId == accountId).ToList();

            // Filter by type
            if (!string.IsNullOrWhiteSpace(txType) && txType != "All")
                transactions = transactions.Where(t => t.Type.Equals(txType, StringComparison.OrdinalIgnoreCase)).ToList();

            var totalIncome  = transactions.Where(t => t.Type == "income") .Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Type == "expense").Sum(t => t.Amount);

            var today      = DateTime.UtcNow.Date;
            var todayCount = transactions.Count(t => t.Date.Date == today);

            var cutoff     = DateTime.UtcNow.AddDays(-30);
            var avgDaily   = transactions
                .Where(t => t.Type == "expense" && t.Date >= cutoff)
                .Sum(t => t.Amount) / 30;

            var chartData = period.ToLower() switch
            {
                "daily"   => GetDailyChartData(transactions),
                "weekly"  => GetWeeklyPeriodData(transactions),
                "monthly" => GetMonthlyChartData(transactions),
                "yearly"  => GetYearlyChartData(transactions),
                _         => GetWeeklyPeriodData(transactions)
            };

            return new ReportSummaryResponse
            {
                TotalIncome           = totalIncome,
                TotalExpense          = totalExpense,
                NetSavings            = totalIncome - totalExpense,
                TodayTransactionCount = todayCount,
                AverageDailySpend     = Math.Round(avgDaily, 2),
                TopCategory           = GetTopCategory(transactions),
                LargestExpense        = GetLargestExpense(transactions) is { } le ? MapTxToResponse(le) : null,
                CategoryBreakdown     = GetCategoryBreakdown(transactions),
                ChartData             = chartData
            };
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private static string GetTopCategory(List<Transaction> transactions)
        {
            var catMap = transactions
                .Where(t => t.Type == "expense")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            return catMap?.Category ?? "—";
        }

        private static Transaction? GetLargestExpense(List<Transaction> transactions)
        {
            return transactions
                .Where(t => t.Type == "expense")
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();
        }

        private static List<CategoryBreakdownItem> GetCategoryBreakdown(List<Transaction> transactions)
        {
            var totalExpense = transactions.Where(t => t.Type == "expense").Sum(t => t.Amount);
            if (totalExpense == 0) return new List<CategoryBreakdownItem>();

            return transactions
                .Where(t => t.Type == "expense")
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Amount)
                .Select((x, i) => new CategoryBreakdownItem
                {
                    Category   = x.Category,
                    Amount     = x.Amount,
                    Percentage = $"{Math.Round((x.Amount / totalExpense) * 100)}%",
                    Color      = ChartColors[i % ChartColors.Length]
                })
                .ToList();
        }

        private static List<WeeklyChartItem> GetWeeklyChartData(List<Transaction> transactions)
        {
            var result = new List<WeeklyChartItem>();
            for (int i = 6; i >= 0; i--)
            {
                var date    = DateTime.UtcNow.Date.AddDays(-i);
                var dayTxs  = transactions.Where(t => t.Date.Date == date).ToList();
                result.Add(new WeeklyChartItem
                {
                    Day     = date.ToString("ddd"),
                    Date    = date.ToString("yyyy-MM-dd"),
                    Income  = dayTxs.Where(t => t.Type == "income") .Sum(t => t.Amount),
                    Expense = dayTxs.Where(t => t.Type == "expense").Sum(t => t.Amount)
                });
            }
            return result;
        }

        private static List<PeriodChartItem> GetDailyChartData(List<Transaction> transactions)
        {
            return Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .Select(date =>
                {
                    var dayTxs = transactions.Where(t => t.Date.Date == date).ToList();
                    return new PeriodChartItem
                    {
                        Label   = date.ToString("ddd"),
                        Income  = dayTxs.Where(t => t.Type == "income") .Sum(t => t.Amount),
                        Expense = dayTxs.Where(t => t.Type == "expense").Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static List<PeriodChartItem> GetWeeklyPeriodData(List<Transaction> transactions)
        {
            return Enumerable.Range(0, 4)
                .Select(i =>
                {
                    var weekStart = DateTime.UtcNow.Date.AddDays(-(3 - i) * 7);
                    var weekEnd   = weekStart.AddDays(7);
                    var weekTxs   = transactions.Where(t => t.Date.Date >= weekStart && t.Date.Date < weekEnd).ToList();
                    return new PeriodChartItem
                    {
                        Label   = $"Week {i + 1}",
                        Income  = weekTxs.Where(t => t.Type == "income") .Sum(t => t.Amount),
                        Expense = weekTxs.Where(t => t.Type == "expense").Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static List<PeriodChartItem> GetMonthlyChartData(List<Transaction> transactions)
        {
            return Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var month    = DateTime.UtcNow.AddMonths(-(5 - i));
                    var monthTxs = transactions
                        .Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month)
                        .ToList();
                    return new PeriodChartItem
                    {
                        Label   = month.ToString("MMM"),
                        Income  = monthTxs.Where(t => t.Type == "income") .Sum(t => t.Amount),
                        Expense = monthTxs.Where(t => t.Type == "expense").Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static List<PeriodChartItem> GetYearlyChartData(List<Transaction> transactions)
        {
            return Enumerable.Range(0, 3)
                .Select(i =>
                {
                    var year    = DateTime.UtcNow.Year - (2 - i);
                    var yearTxs = transactions.Where(t => t.Date.Year == year).ToList();
                    return new PeriodChartItem
                    {
                        Label   = year.ToString(),
                        Income  = yearTxs.Where(t => t.Type == "income") .Sum(t => t.Amount),
                        Expense = yearTxs.Where(t => t.Type == "expense").Sum(t => t.Amount)
                    };
                })
                .ToList();
        }

        private static TransactionResponse MapTxToResponse(Transaction t) => new()
        {
            Id          = t.Id,
            AccountId   = t.AccountId,
            Name        = t.Name,
            Category    = t.Category,
            Amount      = t.Amount,
            Type        = t.Type,
            Description = t.Description,
            Status      = t.Status,
            Date        = t.Date,
            CreatedAt   = t.CreatedAt
        };
    }
}
