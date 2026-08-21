using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly GoogleSheetsClient _sheets;
        private readonly LocalFileStorage _local;
        private const string LocalKey = "orders";

        public OrderRepository(GoogleSheetsClient sheets, LocalFileStorage local)
        {
            _sheets = sheets;
            _local = local;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                return await _local.ReadListAsync<Order>(LocalKey);
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.OrdersSheet);
                var list = new List<Order>();
                for (int i = 1; i < rows.Count; i++)
                {
                    list.Add(MapRowToOrder(rows[i]));
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading orders from Google Sheets, falling back to local: {ex.Message}");
                return await _local.ReadListAsync<Order>(LocalKey);
            }
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            var all = await GetAllAsync();
            return all.Where(o => o.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<Order?> GetByIdAsync(string id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(o => o.Id == id);
        }

        public async Task<Order> CreateAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.Id))
            {
                order.Id = "SG" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            }

            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<Order>(LocalKey);
                all.Insert(0, order);
                await _local.WriteListAsync(LocalKey, all);
                return order;
            }

            try
            {
                await _sheets.AppendRowAsync(GoogleSheetsClient.OrdersSheet, MapOrderToRow(order));
                
                // Write to local as well
                var localList = await _local.ReadListAsync<Order>(LocalKey);
                localList.Insert(0, order);
                await _local.WriteListAsync(LocalKey, localList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error appending order to Google Sheets, using local: {ex.Message}");
                var all = await _local.ReadListAsync<Order>(LocalKey);
                all.Insert(0, order);
                await _local.WriteListAsync(LocalKey, all);
            }

            return order;
        }

        public async Task<Order> UpdateStatusAsync(string id, string status)
        {
            Order? order = null;

            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<Order>(LocalKey);
                order = all.FirstOrDefault(o => o.Id == id);
                if (order != null)
                {
                    order.Status = status;
                    await _local.WriteListAsync(LocalKey, all);
                    return order;
                }
                throw new KeyNotFoundException($"Order '{id}' not found in local storage.");
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.OrdersSheet);
                for (int i = 1; i < rows.Count; i++)
                {
                    if (GetCell(rows[i], 0) == id)
                    {
                        order = MapRowToOrder(rows[i]);
                        order.Status = status;
                        await _sheets.UpdateRowAsync(GoogleSheetsClient.OrdersSheet, i + 1, MapOrderToRow(order));

                        // Update local list
                        var localList = await _local.ReadListAsync<Order>(LocalKey);
                        var localOrder = localList.FirstOrDefault(o => o.Id == id);
                        if (localOrder != null)
                        {
                            localOrder.Status = status;
                            await _local.WriteListAsync(LocalKey, localList);
                        }

                        return order;
                    }
                }
                throw new KeyNotFoundException($"Order '{id}' not found in Google Sheets.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status in Google Sheets, using local: {ex.Message}");
                var all = await _local.ReadListAsync<Order>(LocalKey);
                order = all.FirstOrDefault(o => o.Id == id);
                if (order != null)
                {
                    order.Status = status;
                    await _local.WriteListAsync(LocalKey, all);
                    return order;
                }
                throw new KeyNotFoundException($"Order '{id}' not found.");
            }
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapOrderToRow(Order o) => new List<object>
        {
            o.Id,
            o.UserId,
            JsonSerializer.Serialize(o.Items),
            o.Subtotal.ToString(),
            o.Gst.ToString(),
            o.Total.ToString(),
            o.Name,
            o.Email,
            o.Phone,
            o.Address,
            o.City,
            o.State,
            o.Pincode,
            o.Notes ?? string.Empty,
            o.Status,
            o.PaymentMethod,
            o.PaymentScreenshotUrl,
            o.PlacedAt.ToString("o")
        };

        private static Order MapRowToOrder(IList<object> row)
        {
            var itemsJson = GetCell(row, 2);
            var items = new List<OrderItem>();
            try
            {
                if (!string.IsNullOrWhiteSpace(itemsJson))
                {
                    items = JsonSerializer.Deserialize<List<OrderItem>>(itemsJson) ?? items;
                }
            }
            catch {}

            return new Order
            {
                Id = GetCell(row, 0),
                UserId = GetCell(row, 1),
                Items = items,
                Subtotal = double.TryParse(GetCell(row, 3), out var sub) ? sub : 0,
                Gst = double.TryParse(GetCell(row, 4), out var gst) ? gst : 0,
                Total = double.TryParse(GetCell(row, 5), out var tot) ? tot : 0,
                Name = GetCell(row, 6),
                Email = GetCell(row, 7),
                Phone = GetCell(row, 8),
                Address = GetCell(row, 9),
                City = GetCell(row, 10),
                State = GetCell(row, 11),
                Pincode = GetCell(row, 12),
                Notes = GetCell(row, 13).IfEmptyNull(),
                Status = GetCell(row, 14),
                PaymentMethod = GetCell(row, 15).IfEmpty("upi"),
                PaymentScreenshotUrl = GetCell(row, 16),
                PlacedAt = DateTime.TryParse(GetCell(row, 17), out var pl) ? pl : DateTime.UtcNow
            };
        }

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }
}
