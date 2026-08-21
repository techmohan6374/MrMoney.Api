using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MrMoney.Api.Models;

namespace MrMoney.Api.Infrastructure
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;

        // The /exec URL you get after deploying OrderEmailApi.gs as a Web App
        private const string AppsScriptUrl = "https://script.google.com/macros/s/AKfycbw4hCHRCdyP66NwJB5u_O4ssrAjr7qBhqYzfw_jZnZt91XK6tgwerzrmDB9oTZaZKJ0/exec";

        // Must match SHARED_SECRET in the Apps Script file exactly
        private const string SharedSecret = "d268dc82-568c-4e61-969d-925accd851c7";

        public EmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendNewOrderEmailAsync(Order order)
        {
            var payload = new
            {
                secret = SharedSecret,
                order = new
                {
                    id = order.Id,
                    total = order.Total,
                    subtotal = order.Subtotal,
                    gst = order.Gst,
                    placedAt = order.PlacedAt.ToString("f"),
                    name = order.Name,
                    email = order.Email,
                    phone = order.Phone,
                    address = order.Address,
                    city = order.City,
                    state = order.State,
                    pincode = order.Pincode,
                    notes = order.Notes,
                    items = order.Items?.ConvertAll(i => new
                    {
                        name = i.Name,
                        qty = i.Qty,
                        price = i.Price,
                        image = i.Image
                    })
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            try
            {
                var response = await _httpClient.PostAsync(AppsScriptUrl, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[EmailService] Apps Script call failed: {response.StatusCode} - {responseBody}");
                    return;
                }

                Console.WriteLine($"[EmailService] Apps Script response: {responseBody}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Failed to reach Apps Script API: {ex.Message}");
            }
        }
    }
}