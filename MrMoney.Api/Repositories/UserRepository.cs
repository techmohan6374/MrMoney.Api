using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public UserRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        private static bool IsHeaderRow(IList<object> row)
        {
            if (row == null || row.Count == 0) return false;
            var col0 = GetCell(row, 0);
            var col1 = GetCell(row, 1);
            var col2 = GetCell(row, 2);

            return col0.Equals("Id", StringComparison.OrdinalIgnoreCase)
                || col0.Equals("UserId", StringComparison.OrdinalIgnoreCase)
                || col0.Equals("User Id", StringComparison.OrdinalIgnoreCase)
                || col1.Equals("Email", StringComparison.OrdinalIgnoreCase)
                || col2.Equals("Name", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<UserProfile>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            var list = new List<UserProfile>();
            if (rows.Count > 0)
            {
                var startIdx = IsHeaderRow(rows[0]) ? 1 : 0;
                for (int i = startIdx; i < rows.Count; i++)
                {
                    var user = MapRowToUser(rows[i]);
                    if (!string.IsNullOrWhiteSpace(user.Id) || !string.IsNullOrWhiteSpace(user.Email))
                    {
                        list.Add(user);
                    }
                }
            }
            return list;
        }

        public async Task<UserProfile?> GetByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var all = await GetAllAsync();
            var target = userId.Trim();
            return all.FirstOrDefault(u => u.Id.Trim().Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var all = await GetAllAsync();
            var target = email.Trim();
            return all.FirstOrDefault(u => u.Email.Trim().Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<UserProfile> CreateAsync(UserProfile user)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            await _sheets.AppendRowAsync(GoogleSheetsClient.UsersSheet, MapUserToRow(user));
            return user;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile user)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            var startIdx = (rows.Count > 0 && IsHeaderRow(rows[0])) ? 1 : 0;
            var cleanId = user.Id?.Trim() ?? string.Empty;
            var cleanEmail = user.Email?.Trim() ?? string.Empty;

            for (int i = startIdx; i < rows.Count; i++)
            {
                var rowId = GetCell(rows[i], 0);
                var rowEmail = GetCell(rows[i], 1);

                bool idMatches = !string.IsNullOrEmpty(cleanId) && rowId.Equals(cleanId, StringComparison.OrdinalIgnoreCase);
                bool emailMatches = !string.IsNullOrEmpty(cleanEmail) && rowEmail.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase);

                if (idMatches || emailMatches)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.UsersSheet, i + 1, MapUserToRow(user));
                    return user;
                }
            }
            throw new KeyNotFoundException($"User '{user.Id}' / '{user.Email}' not found in Google Sheets.");
        }

        public async Task DeleteAsync(string userId)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            var startIdx = (rows.Count > 0 && IsHeaderRow(rows[0])) ? 1 : 0;
            var cleanId = userId?.Trim() ?? string.Empty;

            for (int i = startIdx; i < rows.Count; i++)
            {
                var rowId = GetCell(rows[i], 0);
                if (!string.IsNullOrEmpty(cleanId) && rowId.Equals(cleanId, StringComparison.OrdinalIgnoreCase))
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.UsersSheet, i + 1);
                    return;
                }
            }
            throw new KeyNotFoundException($"User '{userId}' not found in Google Sheets.");
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapUserToRow(UserProfile u) => new List<object>
        {
            u.Id?.Trim() ?? string.Empty,
            u.Email?.Trim() ?? string.Empty,
            u.Name?.Trim() ?? string.Empty,
            u.Picture?.Trim() ?? string.Empty,
            u.Role?.Trim() ?? "user",
            u.Provider?.Trim() ?? "google",
            u.JoinedAt.ToString("o"),
            u.LastLoginAt.ToString("o")
        };

        private static UserProfile MapRowToUser(IList<object> row) => new()
        {
            Id = GetCell(row, 0),
            Email = GetCell(row, 1),
            Name = GetCell(row, 2),
            Picture = GetCell(row, 3).IfEmptyNull(),
            Role = GetCell(row, 4).IfEmpty("user"),
            Provider = GetCell(row, 5).IfEmpty("google"),
            JoinedAt = DateTime.TryParse(GetCell(row, 6), out var ja) ? ja : DateTime.UtcNow,
            LastLoginAt = DateTime.TryParse(GetCell(row, 7), out var la) ? la : DateTime.UtcNow
        };

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString()?.Trim() ?? string.Empty : string.Empty;
    }
}
