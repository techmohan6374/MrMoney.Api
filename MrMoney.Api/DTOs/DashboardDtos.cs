namespace MrMoney.Api.DTOs
{
    public class DashboardSummaryResponse
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }
        public int TodayTransactionCount { get; set; }
        public decimal AverageDailySpend { get; set; }
        public string TopCategory { get; set; } = "—";
        public TransactionResponse? LargestExpense { get; set; }
        public List<CategoryBreakdownItem> CategoryBreakdown { get; set; } = new();
        public List<WeeklyChartItem> WeeklyChartData { get; set; } = new();
    }

    public class CategoryBreakdownItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Percentage { get; set; } = "0%";
        public string Color { get; set; } = "#6B7280";
    }

    public class WeeklyChartItem
    {
        public string Day { get; set; } = string.Empty;       // e.g. "Mon"
        public string Date { get; set; } = string.Empty;      // e.g. "2024-01-15"
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class ReportSummaryResponse
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetSavings { get; set; }
        public int TodayTransactionCount { get; set; }
        public decimal AverageDailySpend { get; set; }
        public string TopCategory { get; set; } = "—";
        public TransactionResponse? LargestExpense { get; set; }
        public List<CategoryBreakdownItem> CategoryBreakdown { get; set; } = new();
        public List<PeriodChartItem> ChartData { get; set; } = new();
    }

    public class PeriodChartItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}
