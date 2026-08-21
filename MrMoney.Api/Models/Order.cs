using System;
using System.Collections.Generic;

namespace MrMoney.Api.Models
{
    public class OrderItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Qty { get; set; }
        public string Image { get; set; } = string.Empty;
    }

    public class Order
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public double Subtotal { get; set; }
        public double Gst { get; set; }
        public double Total { get; set; }

        // Shipping / Delivery Info
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Payment screenshot and verification
        public string Status { get; set; } = "Pending Verification"; // Pending Verification, Placed, Rejected
        public string PaymentMethod { get; set; } = "upi";
        public string PaymentScreenshotUrl { get; set; } = string.Empty;
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    }
}
