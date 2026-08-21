using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MrMoney.Api.Infrastructure
{
    public class CloudinaryClient
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _folder;

        public CloudinaryClient(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey    = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];
            _folder       = configuration["Cloudinary:Folder"] ?? "stargraphix";

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string? previousPublicId = null)
        {
            // Delete previous image if exists
            if (!string.IsNullOrWhiteSpace(previousPublicId))
            {
                try
                {
                    await _cloudinary.DestroyAsync(new DeletionParams(previousPublicId));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete old image {previousPublicId}: {ex.Message}");
                }
            }

            // Append random suffix to avoid collisions
            var publicId = Path.GetFileNameWithoutExtension(fileName) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            var uploadParams = new ImageUploadParams()
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = _folder,
                PublicId       = publicId
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public static string? ExtractPublicId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.Contains("cloudinary.com")) return null;
            
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                int uploadIdx = Array.FindIndex(parts, p => p == "upload" || p == "authenticated");
                if (uploadIdx == -1 || parts.Length <= uploadIdx + 2) return null;
                
                int startIdx = parts[uploadIdx + 1].StartsWith("v") ? uploadIdx + 2 : uploadIdx + 1;
                
                var publicIdWithExt = string.Join("/", parts.Skip(startIdx));
                return Path.ChangeExtension(publicIdWithExt, null);
            }
            catch
            {
                return null;
            }
        }
    }
}
