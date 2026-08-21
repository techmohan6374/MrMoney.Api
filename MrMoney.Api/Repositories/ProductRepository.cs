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
        private readonly LocalFileStorage _local;
        private const string LocalKey = "products";

        public ProductRepository(GoogleSheetsClient sheets, LocalFileStorage local)
        {
            _sheets = sheets;
            _local = local;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            List<Product> list;
            if (!_sheets.IsConfigured)
            {
                list = await _local.ReadListAsync<Product>(LocalKey);
            }
            else
            {
                try
                {
                    var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
                    list = new List<Product>();
                    // Row 0 is the header; data starts at row 1
                    for (int i = 1; i < rows.Count; i++)
                    {
                        list.Add(MapRowToProduct(rows[i]));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading products from Google Sheets, falling back to local file: {ex.Message}");
                    list = await _local.ReadListAsync<Product>(LocalKey);
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
                    Name = "Restaurant Menu Flyer",
                    Slug = "restaurant-menu-flyer",
                    Category = "design-printing",
                    Subcategory = "flyers",
                    Price = 549,
                    OriginalPrice = 849,
                    Rating = 4.6,
                    ReviewCount = 67,
                    Image = "/documents/images/Designs/Flyer/5.jpg",
                    Images = new List<string> { "/documents/images/Designs/Flyer/5.jpg", "/documents/images/Designs/Flyer/6.jpg" },
                    Description = "Professional restaurant and café menu flyer designs with elegant food styling.",
                    Features = new List<string> { "Both Sides Design", "Lamination Ready", "Multiple Layouts", "Source File Included" },
                    Tags = new List<string> { "flyer", "restaurant", "menu", "food" },
                    Badge = null,
                    InStock = true
                },
                new Product
                {
                    Id = "p004",
                    Name = "Premium Business Card Design",
                    Slug = "premium-business-card",
                    Category = "design-printing",
                    Subcategory = "business-cards",
                    Price = 299,
                    OriginalPrice = 499,
                    Rating = 4.9,
                    ReviewCount = 256,
                    Image = "/documents/images/Designs/Business Cards/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Business Cards/1.jpg", "/documents/images/Designs/Business Cards/2.jpg" },
                    Description = "Elegant and professional business card designs that make a lasting first impression. Corporate, minimal, or creative styles.",
                    Features = new List<string> { "Standard 3.5×2 inch", "Both Sides Design", "Print-Ready PDF", "Spot UV Ready", "Same Day Delivery" },
                    Tags = new List<string> { "business card", "visiting card", "corporate", "print" },
                    Badge = "Top Rated",
                    InStock = true
                },
                new Product
                {
                    Id = "p005",
                    Name = "Creative Visiting Card",
                    Slug = "creative-visiting-card",
                    Category = "design-printing",
                    Subcategory = "business-cards",
                    Price = 349,
                    OriginalPrice = 599,
                    Rating = 4.7,
                    ReviewCount = 143,
                    Image = "/documents/images/Designs/Business Cards/3.jpg",
                    Images = new List<string> { "/documents/images/Designs/Business Cards/3.jpg" },
                    Description = "Creative and artistic business card designs with unique shapes and layouts for creative professionals.",
                    Features = new List<string> { "Die-Cut Options", "Luxury Paper Options", "Gold Foil Ready", "Custom Shapes" },
                    Tags = new List<string> { "business card", "creative", "luxury" },
                    Badge = null,
                    InStock = true
                },
                new Product
                {
                    Id = "p006",
                    Name = "Professional Resume Design",
                    Slug = "professional-resume",
                    Category = "design-printing",
                    Subcategory = "resumes",
                    Price = 699,
                    OriginalPrice = 1199,
                    Rating = 4.9,
                    ReviewCount = 312,
                    Image = "/documents/images/Designs/Resume/1.jpg",
                    Images = new List<string> { "/documents/images/Designs/Resume/1.jpg", "/documents/images/Designs/Resume/2.jpg" },
                    Description = "ATS-friendly professional resume design that gets you noticed by recruiters. Modern, clean layouts with strong visual hierarchy.",
                    Features = new List<string> { "ATS Compatible", "Word + PDF Format", "Cover Letter Included", "LinkedIn Banner", "5 Color Variants" },
                    Tags = new List<string> { "resume", "cv", "job", "career", "professional" },
                    Badge = "Best Seller",
                    InStock = true
                },
                new Product
                {
                    Id = "p007",
                    Name = "Creative Resume / Portfolio",
                    Slug = "creative-resume-portfolio",
                    Category = "design-printing",
                    Subcategory = "resumes",
                    Price = 899,
                    OriginalPrice = 1499,
                    Rating = 4.8,
                    ReviewCount = 178,
                    Image = "/documents/images/Designs/Resume/3.jpg",
                    Images = new List<string> { "/documents/images/Designs/Resume/3.jpg" },
                    Description = "Creative resume and portfolio design for designers, artists, and creative professionals that stand out.",
                    Features = new List<string> { "2 Page Resume", "Portfolio Pages", "Infographic Style", "Editable in Canva", "Print Ready" },
                    Tags = new List<string> { "resume", "portfolio", "creative", "designer" },
                    Badge = "Premium",
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
                var all = await _local.ReadListAsync<Product>(LocalKey);
                all.Add(product);
                await _local.WriteListAsync(LocalKey, all);
                return product;
            }

            try
            {
                await _sheets.AppendRowAsync(GoogleSheetsClient.ProductsSheet, MapProductToRow(product));
                // Also write to local storage as double-backup/sync
                var localList = await _local.ReadListAsync<Product>(LocalKey);
                localList.Add(product);
                await _local.WriteListAsync(LocalKey, localList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing product to Google Sheets, using local fallback: {ex.Message}");
                var all = await _local.ReadListAsync<Product>(LocalKey);
                all.Add(product);
                await _local.WriteListAsync(LocalKey, all);
            }

            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<Product>(LocalKey);
                var idx = all.FindIndex(p => p.Id == product.Id);
                if (idx != -1)
                {
                    all[idx] = product;
                    await _local.WriteListAsync(LocalKey, all);
                    return product;
                }
                throw new KeyNotFoundException($"Product '{product.Id}' not found in local storage.");
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
                for (int i = 1; i < rows.Count; i++)
                {
                    if (GetCell(rows[i], 0) == product.Id)
                    {
                        await _sheets.UpdateRowAsync(GoogleSheetsClient.ProductsSheet, i + 1, MapProductToRow(product));
                        
                        // Update local as well
                        var localList = await _local.ReadListAsync<Product>(LocalKey);
                        var idx = localList.FindIndex(p => p.Id == product.Id);
                        if (idx != -1)
                        {
                            localList[idx] = product;
                            await _local.WriteListAsync(LocalKey, localList);
                        }
                        
                        return product;
                    }
                }
                throw new KeyNotFoundException($"Product '{product.Id}' not found in Google Sheets.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product in Google Sheets, using local fallback: {ex.Message}");
                var all = await _local.ReadListAsync<Product>(LocalKey);
                var idx = all.FindIndex(p => p.Id == product.Id);
                if (idx != -1)
                {
                    all[idx] = product;
                    await _local.WriteListAsync(LocalKey, all);
                    return product;
                }
                throw new KeyNotFoundException($"Product '{product.Id}' not found.");
            }
        }

        public async Task DeleteAsync(string id)
        {
            if (!_sheets.IsConfigured)
            {
                var all = await _local.ReadListAsync<Product>(LocalKey);
                var item = all.FirstOrDefault(p => p.Id == id);
                if (item != null)
                {
                    all.Remove(item);
                    await _local.WriteListAsync(LocalKey, all);
                }
                return;
            }

            try
            {
                var rows = await _sheets.GetAllRowsAsync(GoogleSheetsClient.ProductsSheet);
                for (int i = 1; i < rows.Count; i++)
                {
                    if (GetCell(rows[i], 0) == id)
                    {
                        await _sheets.DeleteRowAsync(GoogleSheetsClient.ProductsSheet, i + 1);
                        
                        // Update local as well
                        var localList = await _local.ReadListAsync<Product>(LocalKey);
                        var item = localList.FirstOrDefault(p => p.Id == id);
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
                Console.WriteLine($"Error deleting product in Google Sheets, using local fallback: {ex.Message}");
                var all = await _local.ReadListAsync<Product>(LocalKey);
                var item = all.FirstOrDefault(p => p.Id == id);
                if (item != null)
                {
                    all.Remove(item);
                    await _local.WriteListAsync(LocalKey, all);
                }
            }
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
