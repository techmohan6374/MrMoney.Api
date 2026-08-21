using System;
using System.Collections.Generic;

namespace MrMoney.Api.Models
{
    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // e.g. "design-printing", "software-development"
        public string Subcategory { get; set; } = string.Empty; // e.g. "flyers", "business-cards"
        public double Price { get; set; }
        public double? OriginalPrice { get; set; }
        public double Rating { get; set; } = 4.5;
        public int ReviewCount { get; set; } = 0;
        public string Image { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public string? Badge { get; set; }
        public bool InStock { get; set; } = true;
    }
}
