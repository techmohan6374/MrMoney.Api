using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace MrMoney.Api.Infrastructure
{
    /// <summary>
    /// Fallback storage that reads/writes data to local JSON files in the "data" folder.
    /// This allows the API to work seamlessly if Google Sheets credentials are not configured.
    /// </summary>
    public class LocalFileStorage
    {
        private readonly string _dataFolder;
        private readonly object _lock = new object();

        public LocalFileStorage(IWebHostEnvironment env)
        {
            // Store data in a "data" directory at the root of the project
            _dataFolder = Path.Combine(env.ContentRootPath, "data");
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
        }

        public async Task<List<T>> ReadListAsync<T>(string key)
        {
            var filePath = Path.Combine(_dataFolder, $"{key.ToLower()}.json");
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading local storage for {key}: {ex.Message}");
                return new List<T>();
            }
        }

        public async Task WriteListAsync<T>(string key, List<T> list)
        {
            var filePath = Path.Combine(_dataFolder, $"{key.ToLower()}.json");
            try
            {
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                lock (_lock)
                {
                    File.WriteAllText(filePath, json);
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing local storage for {key}: {ex.Message}");
            }
        }
    }
}
