using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    /// <summary>
    /// Reads and writes UserProfile records to the "Users" sheet.
    /// Sheet columns (1-based): Id | Email | Name | Picture | Currency | EmailNotifications | Theme | CreatedAt | LastLoginAt
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public UserRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public async Task<UserProfile?> GetByIdAsync(string userId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            // Row 0 is the header; data starts at row 1
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 0) == userId)
                    return MapRowToUser(row);
            }
            return null;
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (GetCell(row, 1).Equals(email, StringComparison.OrdinalIgnoreCase))
                    return MapRowToUser(row);
            }
            return null;
        }

        // ── Write ────────────────────────────────────────────────────────────

        public async Task<UserProfile> CreateAsync(UserProfile user)
        {
            await _sheets.AppendRowAsync(GoogleSheetsClient.UsersSheet, MapUserToRow(user));
            return user;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile user)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == user.Id)
                {
                    // Sheet row index is 1-based; row 0 = header = sheet row 1, data row i = sheet row i+1
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.UsersSheet, i + 1, MapUserToRow(user));
                    return user;
                }
            }
            throw new KeyNotFoundException($"User '{user.Id}' not found.");
        }

        public async Task DeleteAsync(string userId)
        {
            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == userId)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.UsersSheet, i + 1);
                    return;
                }
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapUserToRow(UserProfile u) => new List<object>
        {
            u.Id,
            u.Email,
            u.Name,
            u.Picture,
            u.Currency,
            u.EmailNotifications.ToString(),
            u.Theme,
            u.CreatedAt.ToString("o"),
            u.LastLoginAt.ToString("o")
        };

        private static UserProfile MapRowToUser(IList<object> row) => new()
        {
            Id                 = GetCell(row, 0),
            Email              = GetCell(row, 1),
            Name               = GetCell(row, 2),
            Picture            = GetCell(row, 3),
            Currency           = GetCell(row, 4).IfEmpty("INR"),
            EmailNotifications = bool.TryParse(GetCell(row, 5), out var en) && en,
            Theme              = GetCell(row, 6).IfEmpty("light"),
            CreatedAt          = DateTime.TryParse(GetCell(row, 7), out var ca) ? ca : DateTime.UtcNow,
            LastLoginAt        = DateTime.TryParse(GetCell(row, 8), out var la) ? la : DateTime.UtcNow
        };

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }

    internal static class StringExtensions
    {
        public static string IfEmpty(this string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
