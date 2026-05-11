using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    /// <summary>
    /// Reads and writes Transaction records to the "Transactions" sheet.
    /// Sheet columns (1-based): Id | UserId | AccountId | Name | Category | Amount | Type | Description | Status | Date | CreatedAt
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public TransactionRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<List<Transaction>> GetAllByUserAsync(string userId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.TransactionsSheet);
            var result = new List<Transaction>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 1) == userId)
                    result.Add(MapRowToTransaction(row));
            }

            return result.OrderByDescending(t => t.Date).ToList();
        }

        public async Task<Transaction?> GetByIdAsync(string userId, string transactionId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.TransactionsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 0) == transactionId && GetCell(row, 1) == userId)
                    return MapRowToTransaction(row);
            }
            return null;
        }

        // ── Write ────────────────────────────────────────────────────────────

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            await _sheets.AppendRowAsync(GoogleSheetsClient.TransactionsSheet, MapTransactionToRow(transaction));
            return transaction;
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.TransactionsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == transaction.Id && GetCell(rows[i], 1) == transaction.UserId)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.TransactionsSheet, i + 1, MapTransactionToRow(transaction));
                    return transaction;
                }
            }
            throw new KeyNotFoundException($"Transaction '{transaction.Id}' not found.");
        }

        public async Task DeleteAsync(string userId, string transactionId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.TransactionsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == transactionId && GetCell(rows[i], 1) == userId)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.TransactionsSheet, i + 1);
                    return;
                }
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapTransactionToRow(Transaction t) => new List<object>
        {
            t.Id,
            t.UserId,
            t.AccountId,
            t.Name,
            t.Category,
            t.Amount.ToString("F2"),
            t.Type,
            t.Description,
            t.Status,
            t.Date.ToString("o"),
            t.CreatedAt.ToString("o")
        };

        private static Transaction MapRowToTransaction(IList<object> row) => new()
        {
            Id          = GetCell(row, 0),
            UserId      = GetCell(row, 1),
            AccountId   = GetCell(row, 2),
            Name        = GetCell(row, 3),
            Category    = GetCell(row, 4),
            Amount      = decimal.TryParse(GetCell(row, 5), out var amt) ? amt : 0,
            Type        = GetCell(row, 6),
            Description = GetCell(row, 7),
            Status      = GetCell(row, 8).IfEmpty("Completed"),
            Date        = DateTime.TryParse(GetCell(row, 9), out var d) ? d : DateTime.UtcNow,
            CreatedAt   = DateTime.TryParse(GetCell(row, 10), out var ca) ? ca : DateTime.UtcNow
        };

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }
}
