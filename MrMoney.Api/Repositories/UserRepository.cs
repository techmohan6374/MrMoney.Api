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
                var startIdx = GetCell(rows[0], 0).Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                for (int i = startIdx; i < rows.Count; i++)
                {
                    list.Add(MapRowToUser(rows[i]));
                }
            }
            return list;
        }

        public async Task<UserProfile?> GetByIdAsync(string userId)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(u => u.Id == userId);
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
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
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == user.Id)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.UsersSheet, i + 1, MapUserToRow(user));
                    return user;
                }
            }
            throw new KeyNotFoundException($"User '{user.Id}' not found in Google Sheets.");
        }

        public async Task DeleteAsync(string userId)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == userId)
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
            u.Id,
            u.Email,
            u.Name,
            u.Picture ?? string.Empty,
            u.Role,
            u.Provider,
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
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }
}
