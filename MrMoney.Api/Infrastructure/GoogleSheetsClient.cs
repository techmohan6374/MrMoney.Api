using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MrMoney.Api.Infrastructure
{
    /// <summary>
    /// Low-level wrapper around the Google Sheets API v4.
    /// Provides read/write/append/delete operations on a single spreadsheet.
    /// Supports a fallback mode if credentials are not configured.
    /// </summary>
    public class GoogleSheetsClient
    {
        // ── Sheet tab name constants ──────────────────────────────────────────
        public const string UsersSheet = "Users";
        public const string ProductsSheet = "Products";
        public const string OrdersSheet = "Orders";
        public const string AdminEmailsSheet = "AdminEmails";

        private readonly SheetsService? _service;
        private readonly string _spreadsheetId;

        public bool IsConfigured { get; }

        public GoogleSheetsClient(IConfiguration configuration)
        {
            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"] ?? "";

            var projectId = configuration["GoogleServiceAccount:ProjectId"];
            var privateKeyId = configuration["GoogleServiceAccount:PrivateKeyId"];
            var privateKey = configuration["GoogleServiceAccount:PrivateKey"];
            var clientEmail = configuration["GoogleServiceAccount:ClientEmail"];
            var clientId = configuration["GoogleServiceAccount:ClientId"];

            if (string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(_spreadsheetId))
            {
                IsConfigured = false;
                _service = null;
                return;
            }

            try
            {
                privateKey = privateKey.Replace("\\n", "\n");

                var credentialObject = new
                {
                    type = "service_account",
                    project_id = projectId,
                    private_key_id = privateKeyId,
                    private_key = privateKey,
                    client_email = clientEmail,
                    client_id = clientId,
                    auth_uri = "https://accounts.google.com/o/oauth2/auth",
                    token_uri = "https://oauth2.googleapis.com/token",
                    auth_provider_x509_cert_url = "https://www.googleapis.com/oauth2/v1/certs",
                    client_x509_cert_url = $"https://www.googleapis.com/robot/v1/metadata/x509/{Uri.EscapeDataString(clientEmail)}"
                };

                var json = JsonSerializer.Serialize(credentialObject);

                var credential = GoogleCredential
                    .FromJson(json)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                _service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "StarGraphix"
                });

                IsConfigured = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error configuring GoogleSheetsClient: " + ex.Message);
                IsConfigured = false;
                _service = null;
            }
        }

        // ── Read ─────────────────────────────────────────────────────────────

        /// <summary>Returns all rows (including header) from the given sheet.</summary>
        public async Task<IList<IList<object>>> GetAllRowsAsync(string sheetName)
        {
            if (!IsConfigured || _service == null)
                throw new InvalidOperationException("GoogleSheetsClient is not configured.");

            var range = $"{sheetName}!A:Z";
            var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            return response.Values ?? new List<IList<object>>();
        }

        // ── Append ───────────────────────────────────────────────────────────

        /// <summary>Appends a single row to the end of the sheet.</summary>
        public async Task AppendRowAsync(string sheetName, IList<object> rowValues)
        {
            if (!IsConfigured || _service == null)
                throw new InvalidOperationException("GoogleSheetsClient is not configured.");

            var range = $"{sheetName}!A1";
            var body = new ValueRange { Values = new List<IList<object>> { rowValues } };

            var request = _service.Spreadsheets.Values.Append(body, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            await request.ExecuteAsync();
        }

        // ── Update ───────────────────────────────────────────────────────────

        /// <summary>Overwrites a specific row (1-based row index) with new values.</summary>
        public async Task UpdateRowAsync(string sheetName, int rowIndex, IList<object> rowValues)
        {
            if (!IsConfigured || _service == null)
                throw new InvalidOperationException("GoogleSheetsClient is not configured.");

            var range = $"{sheetName}!A{rowIndex}";
            var body = new ValueRange { Values = new List<IList<object>> { rowValues } };

            var request = _service.Spreadsheets.Values.Update(body, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            await request.ExecuteAsync();
        }

        // ── Delete ───────────────────────────────────────────────────────────

        /// <summary>
        /// Deletes a row by its 1-based row index.
        /// Uses the batchUpdate DeleteDimension request so the row is physically removed.
        /// </summary>
        public async Task DeleteRowAsync(string sheetName, int rowIndex)
        {
            if (!IsConfigured || _service == null)
                throw new InvalidOperationException("GoogleSheetsClient is not configured.");

            // First get the sheet ID (numeric) for the named sheet
            var sheetId = await GetSheetIdAsync(sheetName);

            var deleteRequest = new Request
            {
                DeleteDimension = new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = rowIndex - 1, // 0-based
                        EndIndex = rowIndex       // exclusive
                    }
                }
            };

            var batchBody = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request> { deleteRequest }
            };

            await _service.Spreadsheets.BatchUpdate(batchBody, _spreadsheetId).ExecuteAsync();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Returns the numeric sheet ID for a given sheet name.</summary>
        private async Task<int> GetSheetIdAsync(string sheetName)
        {
            if (!IsConfigured || _service == null)
                throw new InvalidOperationException("GoogleSheetsClient is not configured.");

            var spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s =>
                s.Properties.Title.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (sheet == null)
                throw new InvalidOperationException($"Sheet '{sheetName}' not found in spreadsheet.");

            return (int)(sheet.Properties.SheetId ?? 0);
        }

        /// <summary>
        /// Ensures all required sheets exist with their header rows.
        /// Call this once at startup.
        /// </summary>
        public async Task EnsureSheetsExistAsync()
        {
            if (!IsConfigured || _service == null)
            {
                Console.WriteLine("GoogleSheetsClient not configured. Skipping sheets initialization.");
                return;
            }

            var spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            var existingSheets = spreadsheet.Sheets.Select(s => s.Properties.Title).ToHashSet();

            var sheetsToCreate = new Dictionary<string, IList<object>>
            {
                [UsersSheet] = new List<object> { "Id", "Email", "Name", "Picture", "Role", "Provider", "JoinedAt", "LastLoginAt" },
                [ProductsSheet] = new List<object> { "Id", "Name", "Slug", "Category", "Subcategory", "Price", "OriginalPrice", "Rating", "ReviewCount", "Image", "ImagesJson", "Description", "FeaturesJson", "TagsJson", "Badge", "InStock" },
                [OrdersSheet] = new List<object> { "Id", "UserId", "ItemsJson", "Subtotal", "Gst", "Total", "Name", "Email", "Phone", "Address", "City", "State", "Pincode", "Notes", "Status", "PaymentMethod", "PaymentScreenshotUrl", "PlacedAt" },
                [AdminEmailsSheet] = new List<object> { "Email" }
            };

            var addSheetRequests = new List<Request>();
            foreach (var (name, _) in sheetsToCreate)
            {
                if (!existingSheets.Contains(name))
                {
                    addSheetRequests.Add(new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties { Title = name }
                        }
                    });
                }
            }

            if (addSheetRequests.Count > 0)
            {
                await _service.Spreadsheets.BatchUpdate(
                    new BatchUpdateSpreadsheetRequest { Requests = addSheetRequests },
                    _spreadsheetId
                ).ExecuteAsync();
            }

            // Write headers for newly created sheets
            foreach (var (name, headers) in sheetsToCreate)
            {
                if (!existingSheets.Contains(name))
                {
                    var range = $"{name}!A1";
                    var body = new ValueRange { Values = new List<IList<object>> { headers } };
                    var req = _service.Spreadsheets.Values.Update(body, _spreadsheetId, range);
                    req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    await req.ExecuteAsync();
                }
            }
        }
    }
}
