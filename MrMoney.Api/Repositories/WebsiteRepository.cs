using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public class WebsiteRepository : IWebsiteRepository
    {
        private readonly GoogleSheetsClient _sheets;
        private static readonly List<Website> _inMemoryFallback = new();

        public WebsiteRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        public async Task<List<Website>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                if (_inMemoryFallback.Count == 0)
                {
                    _inMemoryFallback.AddRange(GetDefaultWebsites());
                }
                return _inMemoryFallback;
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.WebsitesSheet);
            var list = new List<Website>();

            if (rows.Count > 0)
            {
                var startIdx = GetCell(rows[0], 0).Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                for (int i = startIdx; i < rows.Count; i++)
                {
                    var item = MapRowToWebsite(rows[i]);
                    if (!string.IsNullOrWhiteSpace(item.Id) || !string.IsNullOrWhiteSpace(item.Name))
                    {
                        list.Add(item);
                    }
                }
            }

            if (list.Count == 0)
            {
                var defaults = GetDefaultWebsites();
                foreach (var w in defaults)
                {
                    await CreateAsync(w);
                }
                return defaults;
            }

            return list;
        }

        public async Task<Website?> GetByIdAsync(string id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Website> CreateAsync(Website website)
        {
            if (string.IsNullOrWhiteSpace(website.Id))
            {
                website.Id = "web_" + Guid.NewGuid().ToString("N")[..8];
            }
            if (string.IsNullOrWhiteSpace(website.CreatedAt))
            {
                website.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (!_sheets.IsConfigured)
            {
                _inMemoryFallback.Add(website);
                return website;
            }

            await _sheets.AppendRowAsync(GoogleSheetsClient.WebsitesSheet, MapWebsiteToRow(website));
            return website;
        }

        public async Task<Website> UpdateAsync(string id, Website website)
        {
            website.Id = id;

            if (!_sheets.IsConfigured)
            {
                var idx = _inMemoryFallback.FindIndex(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _inMemoryFallback[idx] = website;
                    return website;
                }
                throw new KeyNotFoundException($"Website '{id}' not found.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.WebsitesSheet);
            var startIdx = rows.Count > 0 && GetCell(rows[0], 0).Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            for (int i = startIdx; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0).Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.WebsitesSheet, i + 1, MapWebsiteToRow(website));
                    return website;
                }
            }

            throw new KeyNotFoundException($"Website '{id}' not found in Google Sheets.");
        }

        public async Task DeleteAsync(string id)
        {
            if (!_sheets.IsConfigured)
            {
                _inMemoryFallback.RemoveAll(w => w.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                return;
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.WebsitesSheet);
            var startIdx = rows.Count > 0 && GetCell(rows[0], 0).Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            for (int i = startIdx; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0).Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.WebsitesSheet, i + 1);
                    return;
                }
            }

            throw new KeyNotFoundException($"Website '{id}' not found in Google Sheets.");
        }

        private static List<Website> GetDefaultWebsites()
        {
            return new List<Website>
            {
                new Website
                {
                    Id = "web_001",
                    Name = "Star Graphix Official Portal",
                    Url = "https://stargraphix.com",
                    Description = "Official digital portal for high-end graphic design, commercial printing, resumes, wedding cards, and custom software development.",
                    Category = "Official",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                },
                new Website
                {
                    Id = "web_002",
                    Name = "Star Graphix Free Creative Tools Hub",
                    Url = "https://stargraphix.com/free-tools",
                    Description = "Online suite of tools including QR code generator, barcode generator, PDF editor, and age calculator.",
                    Category = "Tools",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                },
                new Website
                {
                    Id = "web_003",
                    Name = "Client Design Showcases & Portfolio",
                    Url = "https://stargraphix.com/portal",
                    Description = "Interactive gallery of past printing projects, brochures, flyers, banners, and logos delivered to 5,000+ satisfied clients.",
                    Category = "Showcase",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        private static IList<object> MapWebsiteToRow(Website w) => new List<object>
        {
            w.Id,
            w.Name,
            w.Url,
            w.Description,
            w.Category,
            w.Username ?? string.Empty,
            w.Password ?? string.Empty,
            w.CreatedAt
        };

        private static Website MapRowToWebsite(IList<object> row)
        {
            // Support both 8-column layout (with Username, Password) and 6-column legacy layout
            var hasCredentials = row.Count >= 8;
            return new Website
            {
                Id = GetCell(row, 0),
                Name = GetCell(row, 1),
                Url = GetCell(row, 2),
                Description = GetCell(row, 3),
                Category = string.IsNullOrWhiteSpace(GetCell(row, 4)) ? "Main" : GetCell(row, 4),
                Username = hasCredentials ? GetCell(row, 5) : string.Empty,
                Password = hasCredentials ? GetCell(row, 6) : string.Empty,
                CreatedAt = hasCredentials ? GetCell(row, 7) : GetCell(row, 5)
            };
        }

        private static string GetCell(IList<object> row, int index)
        {
            if (index >= row.Count || row[index] == null) return string.Empty;
            return row[index].ToString()?.Trim() ?? string.Empty;
        }
    }
}
