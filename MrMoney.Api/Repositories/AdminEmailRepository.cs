using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MrMoney.Api.Infrastructure;

namespace MrMoney.Api.Repositories
{
    public class AdminEmailRepository : IAdminEmailRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public AdminEmailRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        public async Task<List<string>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AdminEmailsSheet);
            var list = new List<string>();
            if (rows.Count > 0)
            {
                var startIdx = rows[0].Count > 0 && rows[0][0]?.ToString()?.Equals("Email", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0;
                for (int i = startIdx; i < rows.Count; i++)
                {
                    if (rows[i].Count > 0)
                    {
                        var email = rows[i][0]?.ToString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            list.Add(email);
                        }
                    }
                }
            }

            if (list.Count == 0)
            {
                var defaultEmail = "dwaynejohnsonjohnson89@gmail.com";
                await AddAsync(defaultEmail);
                list.Add(defaultEmail);
            }

            return list;
        }

        public async Task AddAsync(string email)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            await _sheets.AppendRowAsync(GoogleSheetsClient.AdminEmailsSheet, new List<object> { email });
        }

        public async Task DeleteAsync(string email)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.AdminEmailsSheet);
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Count > 0 && rows[i][0]?.ToString()?.Equals(email, StringComparison.OrdinalIgnoreCase) == true)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.AdminEmailsSheet, i + 1);
                    return;
                }
            }
            throw new KeyNotFoundException($"Email '{email}' not found in Google Sheets.");
        }
    }
}
