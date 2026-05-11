using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

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
            _folder       = configuration["Cloudinary:Folder"] ?? "avatars";

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadAvatarAsync(Stream fileStream, string fileName, string? previousPublicId = null)
        {
            // Delete previous image if exists
            if (!string.IsNullOrWhiteSpace(previousPublicId))
            {
                await _cloudinary.DestroyAsync(new DeletionParams(previousPublicId));
            }

            var uploadParams = new ImageUploadParams()
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = _folder,
                PublicId       = Path.GetFileNameWithoutExtension(fileName),
                Transformation = new Transformation().Width(256).Height(256).Crop("fill").Gravity("face")
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
            
            // Example: https://res.cloudinary.com/dk1i8ceah/image/upload/v1715458532/mr-money-profiles/avatar_123.jpg
            // PublicId would be "mr-money-profiles/avatar_123"
            
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath; // /dk1i8ceah/image/upload/v1715458532/mr-money-profiles/avatar_123.jpg
                
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                // Parts: [dk1i8ceah, image, upload, v1715458532, mr-money-profiles, avatar_123.jpg]
                
                // Find the index of "upload" or "authenticated"
                int uploadIdx = Array.FindIndex(parts, p => p == "upload" || p == "authenticated");
                if (uploadIdx == -1 || parts.Length <= uploadIdx + 2) return null;
                
                // Skip the version part (starts with 'v') if it exists
                int startIdx = parts[uploadIdx + 1].StartsWith("v") ? uploadIdx + 2 : uploadIdx + 1;
                
                var publicIdWithExt = string.Join("/", parts.Skip(startIdx)); // mr-money-profiles/avatar_123.jpg
                return Path.ChangeExtension(publicIdWithExt, null); // mr-money-profiles/avatar_123
            }
            catch
            {
                return null;
            }
        }
    }
}
