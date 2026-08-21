using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly GoogleSheetsClient _sheets;

        public ProductRepository(GoogleSheetsClient sheets)
        {
            _sheets = sheets;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
            var list = new List<Product>();
            
            if (rows.Count > 0)
            {
                var startIdx = GetCell(rows[0], 0).Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                for (int i = startIdx; i < rows.Count; i++)
                {
                    list.Add(MapRowToProduct(rows[i]));
                }
            }

            if (list.Count == 0)
            {
                var defaults = GetDefaultProducts();
                foreach (var p in defaults)
                {
                    await CreateAsync(p);
                }
                return defaults;
            }

            return list;
        }

        private static List<Product> GetDefaultProducts()
        {
            return new List<Product>
            {
                new Product
                {
                    Id = "p001",
                    Name = "Event Promotional Flyer",
                    Slug = "event-promotional-flyer",
                    Category = "design-printing",
                    Subcategory = "flyers",
                    Price = 499,
                    OriginalPrice = 799,
                    Rating = 4.8,
                    ReviewCount = 124,
                    Image = "/documents/images/Designs/Flyer/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Flyer/1.jpg", "/documents/images/Designs/Flyer/2.jpg" },
                    Description = "Eye-catching promotional flyers for events, parties, and special occasions. High-resolution print-ready files delivered within 24 hours.",
                    Features = new List<string> { "A4 & A5 Sizes", "Print-Ready PDF", "300 DPI Resolution", "24hr Delivery", "Unlimited Revisions" },
                    Tags = new List<string> { "flyer", "event", "promotion", "print" },
                    Badge = "Best Seller",
                    InStock = true
                },
                new Product
                {
                    Id = "p002",
                    Name = "Festival Sale Flyer",
                    Slug = "festival-sale-flyer",
                    Category = "design-printing",
                    Subcategory = "flyers",
                    Price = 399,
                    OriginalPrice = 649,
                    Rating = 4.7,
                    ReviewCount = 89,
                    Image = "/documents/images/Designs/Flyer/3.jpg",
                    Images = new List<string> { "/documents/images/Designs/Flyer/3.jpg", "/documents/images/Designs/Flyer/4.jpg" },
                    Description = "Vibrant festival and seasonal sale flyers designed to attract maximum attention.",
                    Features = new List<string> { "Multiple Size Options", "Vibrant Colors", "Editable PSD Included", "48hr Delivery" },
                    Tags = new List<string> { "flyer", "festival", "sale", "print" },
                    Badge = "Popular",
                    InStock = true
                },
                new Product
                {
                    Id = "p003",
                    Name = "Corporate Business Card",
                    Slug = "corporate-business-card",
                    Category = "design-printing",
                    Subcategory = "business-cards",
                    Price = 299,
                    OriginalPrice = 499,
                    Rating = 4.9,
                    ReviewCount = 156,
                    Image = "/documents/images/Designs/Business Cards/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Business Cards/1.jpg", "/documents/images/Designs/Business Cards/2.jpg" },
                    Description = "Professional double-sided business card designs tailored to your corporate identity.",
                    Features = new List<string> { "Double-Sided Design", "Standard 3.5\"x2\" Size", "CMYK Print-Ready PDF", "Source Vector Files" },
                    Tags = new List<string> { "business card", "corporate", "identity", "print" },
                    Badge = "Top Rated",
                    InStock = true
                },
                new Product
                {
                    Id = "p004",
                    Name = "Vibrant Digital Visiting Card",
                    Slug = "vibrant-digital-visiting-card",
                    Category = "design-printing",
                    Subcategory = "business-cards",
                    Price = 199,
                    OriginalPrice = 349,
                    Rating = 4.6,
                    ReviewCount = 74,
                    Image = "/documents/images/Designs/Business Cards/3.jpg",
                    Images = new List<string> { "/documents/images/Designs/Business Cards/3.jpg" },
                    Description = "Vibrant digital cards perfect for sharing on WhatsApp or email.",
                    Features = new List<string> { "Tap-to-Action Buttons", "Vibrant Digital Format", "Shareable PDF/JPG", "Interactive Links" },
                    Tags = new List<string> { "digital card", "visiting card", "whatsapp", "interactive" },
                    Badge = "Best Value",
                    InStock = true
                },
                new Product
                {
                    Id = "p005",
                    Name = "Modern Professional Resume",
                    Slug = "modern-professional-resume",
                    Category = "design-printing",
                    Subcategory = "resumes",
                    Price = 349,
                    OriginalPrice = 599,
                    Rating = 4.8,
                    ReviewCount = 112,
                    Image = "/documents/images/Designs/Resume/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Resume/1.jpg", "/documents/images/Designs/Resume/2.jpg" },
                    Description = "ATS-friendly modern resume designs to help you land your dream job.",
                    Features = new List<string> { "ATS-Optimized Layout", "Editable MS Word & PDF", "Cover Letter Template", "1-Page or 2-Page Options" },
                    Tags = new List<string> { "resume", "cv", "career", "job application" },
                    Badge = "Popular",
                    InStock = true
                },
                new Product
                {
                    Id = "p006",
                    Name = "Creative Infographic CV",
                    Slug = "creative-infographic-cv",
                    Category = "design-printing",
                    Subcategory = "resumes",
                    Price = 449,
                    OriginalPrice = 699,
                    Rating = 4.7,
                    ReviewCount = 67,
                    Image = "/documents/images/Designs/Resume/3.jpg",
                    Images = new List<string> { "/documents/images/Designs/Resume/3.jpg" },
                    Description = "Visual-heavy infographic CVs perfect for creative, design and marketing fields.",
                    Features = new List<string> { "Creative Infographics", "Vibrant Theme Options", "Highly Visual Layout", "Free Icon Pack" },
                    Tags = new List<string> { "cv", "resume", "infographic", "creative cv" },
                    Badge = "Premium",
                    InStock = true
                },
                new Product
                {
                    Id = "p007",
                    Name = "Business Landing Page Web App",
                    Slug = "business-landing-page",
                    Category = "software-development",
                    Subcategory = "web-applications",
                    Price = 9999,
                    OriginalPrice = 14999,
                    Rating = 4.9,
                    ReviewCount = 42,
                    Image = "/documents/images/Websites/Landing Page/1.jpg",
                    Images = new List<string> { "/documents/images/Websites/Landing Page/1.jpg", "/documents/images/Websites/Landing Page/2.jpg" },
                    Description = "High-converting, responsive landing page built with modern frontend frameworks.",
                    Features = new List<string> { "React / Next.js Setup", "Responsive Design", "SEO Optimized", "Contact Form + Analytics", "1 Month Free Support" },
                    Tags = new List<string> { "website", "landing page", "web development", "nextjs" },
                    Badge = "Enterprise",
                    InStock = true
                },
                new Product
                {
                    Id = "p008",
                    Name = "Instagram Post Package (10 Posts)",
                    Slug = "instagram-post-package",
                    Category = "design-printing",
                    Subcategory = "instagram-posters",
                    Price = 1499,
                    OriginalPrice = 2499,
                    Rating = 4.8,
                    ReviewCount = 201,
                    Image = "/documents/images/Designs/Instagram Posters/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Instagram Posters/1.jpg", "/documents/images/Designs/Instagram Posters/2.jpg" },
                    Description = "Professional Instagram post pack of 10 unique designs for your brand. Includes square, portrait and story formats.",
                    Features = new List<string> { "10 Instagram Posts", "Canva Link Included", "High-Resolution JPEGs", "Brand Color Matching" },
                    Tags = new List<string> { "instagram", "social media", "post", "canva" },
                    Badge = "Best Seller",
                    InStock = true
                }
            };
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(p => p.Id == id);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Id))
            {
                product.Id = "p_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            }

            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            await _sheets.AppendRowAsync(GoogleSheetsClient.ProductsSheet, MapProductToRow(product));
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == product.Id)
                {
                    await _sheets.UpdateRowAsync(GoogleSheetsClient.ProductsSheet, i + 1, MapProductToRow(product));
                    return product;
                }
            }
            throw new KeyNotFoundException($"Product '{product.Id}' not found in Google Sheets.");
        }

        public async Task DeleteAsync(string id)
        {
            if (!_sheets.IsConfigured)
            {
                throw new InvalidOperationException("Google Sheets is not configured.");
            }

            var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
            for (int i = 1; i < rows.Count; i++)
            {
                if (GetCell(rows[i], 0) == id)
                {
                    await _sheets.DeleteRowAsync(GoogleSheetsClient.ProductsSheet, i + 1);
                    return;
                }
            }
            throw new KeyNotFoundException($"Product '{id}' not found in Google Sheets.");
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static IList<object> MapProductToRow(Product p) => new List<object>
        {
            p.Id,
            p.Name,
            p.Slug,
            p.Category,
            p.Subcategory,
            p.Price.ToString(),
            p.OriginalPrice?.ToString() ?? string.Empty,
            p.Rating.ToString(),
            p.ReviewCount.ToString(),
            p.Image,
            JsonSerializer.Serialize(p.Images),
            p.Description,
            JsonSerializer.Serialize(p.Features),
            JsonSerializer.Serialize(p.Tags),
            p.Badge ?? string.Empty,
            p.InStock.ToString()
        };

        private static Product MapRowToProduct(IList<object> row)
        {
            var imagesJson = GetCell(row, 10);
            var featuresJson = GetCell(row, 12);
            var tagsJson = GetCell(row, 13);

            List<string> images = new List<string>();
            List<string> features = new List<string>();
            List<string> tags = new List<string>();

            try { if (!string.IsNullOrWhiteSpace(imagesJson)) images = JsonSerializer.Deserialize<List<string>>(imagesJson) ?? images; } catch {}
            try { if (!string.IsNullOrWhiteSpace(featuresJson)) features = JsonSerializer.Deserialize<List<string>>(featuresJson) ?? features; } catch {}
            try { if (!string.IsNullOrWhiteSpace(tagsJson)) tags = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? tags; } catch {}

            return new Product
            {
                Id = GetCell(row, 0),
                Name = GetCell(row, 1),
                Slug = GetCell(row, 2),
                Category = GetCell(row, 3),
                Subcategory = GetCell(row, 4),
                Price = double.TryParse(GetCell(row, 5), out var price) ? price : 0,
                OriginalPrice = double.TryParse(GetCell(row, 6), out var orig) ? orig : (double?)null,
                Rating = double.TryParse(GetCell(row, 7), out var rating) ? rating : 4.5,
                ReviewCount = int.TryParse(GetCell(row, 8), out var reviews) ? reviews : 0,
                Image = GetCell(row, 9),
                Images = images,
                Description = GetCell(row, 11),
                Features = features,
                Tags = tags,
                Badge = GetCell(row, 14).IfEmptyNull(),
                InStock = !bool.TryParse(GetCell(row, 15), out var stock) || stock // defaults to true if failed
            };
        }

        private static string GetCell(IList<object> row, int index)
            => index < row.Count ? row[index]?.ToString() ?? string.Empty : string.Empty;
    }

    internal static class ProductExtensions
    {
        public static string? IfEmptyNull(this string value)
            => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
