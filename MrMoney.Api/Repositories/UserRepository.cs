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
        private readonly LocalFileStorage _local;
        private const string LocalKey = "users";

        public UserRepository(GoogleSheetsClient sheets, LocalFileStorage local)
        {
            _sheets = sheets;
            _local = local;
        }

        public async Task<List<UserProfile>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                return await _local.ReadListAsync<UserProfile>(LocalKey);
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
                var list = new List<UserProfile>();
                for (int i = 1; i < rows.Count; i++)
                {
                    list.Add(MapRowToUser(rows[i]));
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading users from Google Sheets, falling back to local: {ex.Message}");
                return await _local.ReadListAsync<UserProfile>(LocalKey);
            }
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
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                all.Add(user);
                await _local.WriteListAsync(LocalKey, all);
                return user;
            }

            try
            {
                await _sheets.AppendRowAsync(GoogleSheetsClient.UsersSheet, MapUserToRow(user));
                
                // Write to local as well
                var localList = await _local.ReadListAsync<UserProfile>(LocalKey);
                localList.Add(user);
                await _local.WriteListAsync(LocalKey, localList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user in Google Sheets, using local: {ex.Message}");
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                all.Add(user);
                await _local.WriteListAsync(LocalKey, all);
            }

            return user;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile user)
        {
            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                var idx = all.FindIndex(u => u.Id == user.Id);
                if (idx != -1)
                {
                    all[idx] = user;
                    await _local.WriteListAsync(LocalKey, all);
                    return user;
                }
                throw new KeyNotFoundException($"User '{user.Id}' not found in local storage.");
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
                for (int i = 1; i < rows.Count; i++)
                {
                    if (GetCell(rows[i], 0) == user.Id)
                    {
                        await _sheets.UpdateRowAsync(GoogleSheetsClient.UsersSheet, i + 1, MapUserToRow(user));

                        // Sync local
                        var localList = await _local.ReadListAsync<UserProfile>(LocalKey);
                        var idx = localList.FindIndex(u => u.Id == user.Id);
                        if (idx != -1)
                        {
                            localList[idx] = user;
                            await _local.WriteListAsync(LocalKey, localList);
                        }

                        return user;
                    }
                }
                throw new KeyNotFoundException($"User '{user.Id}' not found in Google Sheets.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user in Google Sheets, using local: {ex.Message}");
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                var idx = all.FindIndex(u => u.Id == user.Id);
                if (idx != -1)
                {
                    all[idx] = user;
                    await _local.WriteListAsync(LocalKey, all);
                    return user;
                }
                throw new KeyNotFoundException($"User '{user.Id}' not found.");
            }
        }

        public async Task DeleteAsync(string userId)
        {
            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                var item = all.FirstOrDefault(u => u.Id == userId);
                if (item != null)
                {
                    all.Remove(item);
                    await _local.WriteListAsync(LocalKey, all);
                }
                return;
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.UsersSheet);
                for (int i = 1; i < rows.Count; i++)
                {
                    if (GetCell(rows[i], 0) == userId)
                    {
                        await _sheets.DeleteRowAsync(GoogleSheetsClient.UsersSheet, i + 1);

                        // Sync local
                        var localList = await _local.ReadListAsync<UserProfile>(LocalKey);
                        var item = localList.FirstOrDefault(u => u.Id == userId);
                        if (item != null)
                        {
                            localList.Remove(item);
                            await _local.WriteListAsync(LocalKey, localList);
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user in Google Sheets, using local: {ex.Message}");
                var all = await _local.ReadListAsync<UserProfile>(LocalKey);
                var item = all.FirstOrDefault(u => u.Id == userId);
                if (item != null)
                {
                    all.Remove(item);
                    await _local.WriteListAsync(LocalKey, all);
                }
            }
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
