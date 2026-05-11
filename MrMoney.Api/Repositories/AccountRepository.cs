using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    /// <summary>
    /// Reads and writes Account records to the "Accounts" sheet.
    /// Sheet columns (1-based): Id | UserId | Name | HolderName | Balance | Type | Color | IsDefault | CreatedAt
    /// </summary>
    public class AccountRepository : IAccountRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public AccountRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<List<Account>> GetAllByUserAsync(string userId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AccountsSheet);
            var result = new List<Account>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 1) == userId)
                    result.Add(MapRowToAccount(row));
            }

            return result.OrderByDescending(a => a.CreatedAt).ToList();
        }

        public async Task<Account?> GetByIdAsync(string userId, string accountId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AccountsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 0) == accountId && GetCell(row, 1) == userId)
                    return MapRowToAccount(row);
            }
            return null;
        }

        // ── Write ────────────────────────────────────────────────────────────

        public async Task<Account> CreateAsync(Account account)
        {
            await _sheets.AppendRowAsync(GoogleSheetsClient.AccountsSheet, MapAccountToRow(account));
            return account;
        }

        public async Task<Account> UpdateAsync(Account account)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AccountsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == account.Id && GetCell(rows[i], 1) == account.UserId)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.AccountsSheet, i + 1, MapAccountToRow(account));
                    return account;
                }
            }
            throw new KeyNotFoundException($"Account '{account.Id}' not found.");
        }

        public async Task DeleteAsync(string userId, string accountId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AccountsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == accountId && GetCell(rows[i], 1) == userId)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.AccountsSheet, i + 1);
                    return;
                }
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapAccountToRow(Account a) => new List<object>
        {
            a.Id,
            a.UserId,
            a.Name,
            a.HolderName,
            a.Balance.ToString("F2"),
            a.Type,
            a.Color,
            a.IsDefault.ToString(),
            a.CreatedAt.ToString("o")
        };

        private static Account MapRowToAccount(IList<object> row) => new()
        {
            Id          = GetCell(row, 0),
            UserId      = GetCell(row, 1),
            Name        = GetCell(row, 2),
            HolderName  = GetCell(row, 3),
            Balance     = decimal.TryParse(GetCell(row, 4), out var bal) ? bal : 0,
            Type        = GetCell(row, 5).IfEmpty("Savings"),
            Color       = GetCell(row, 6).IfEmpty("#10B981"),
            IsDefault   = bool.TryParse(GetCell(row, 7), out var def) && def,
            CreatedAt   = DateTime.TryParse(GetCell(row, 8), out var ca) ? ca : DateTime.UtcNow
        };

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }
}
