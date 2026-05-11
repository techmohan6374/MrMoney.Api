using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;

namespace MrMoney.Api.Infrastructure
{
    /// <summary>
    /// Uploads files to Google Drive using the service account.
    /// Uploaded files are made publicly readable so the URL works as an avatar src.
    /// </summary>
    public class GoogleDriveClient
    {
        private readonly DriveService _service;
        private readonly string _avatarFolderId; // optional — empty = Drive root

        public GoogleDriveClient(IConfiguration configuration, IWebHostEnvironment env)
        {
            var configuredPath = configuration["GoogleDrive:ServiceAccountKeyPath"]
                ?? throw new InvalidOperationException("GoogleDrive:ServiceAccountKeyPath is not configured.");

            _avatarFolderId = configuration["GoogleDrive:AvatarFolderId"] ?? string.Empty;
            Console.WriteLine($"[DEBUG] GoogleDriveClient: AvatarFolderId loaded as '{_avatarFolderId}'");


            // Resolve key file path (same logic as GoogleSheetsClient)
            var keyPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(env.ContentRootPath, configuredPath);

            if (!System.IO.File.Exists(keyPath))
            {
                var solutionRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, ".."));
                keyPath = Path.Combine(solutionRoot, configuredPath);
            }

            if (!System.IO.File.Exists(keyPath))
                throw new FileNotFoundException(
                    $"Google service account key not found at '{keyPath}'.");

            GoogleCredential credential;
            using (var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read))
            {
#pragma warning disable CS0618
                credential = GoogleCredential
                    .FromStream(stream)
                    .CreateScoped(DriveService.Scope.Drive);
#pragma warning restore CS0618
            }

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName       = "MrMoney"
            });
        }

        /// <summary>
        /// Uploads an image stream to Google Drive and returns a direct public URL.
        /// If a previous file ID is supplied, it is deleted first.
        /// </summary>
        public async Task<string> UploadAvatarAsync(
            Stream fileStream,
            string fileName,
            string mimeType,
            string? previousFileId = null)
        {
            // Delete old avatar if one exists
            if (!string.IsNullOrWhiteSpace(previousFileId))
            {
                try { await _service.Files.Delete(previousFileId).ExecuteAsync(); }
                catch { /* ignore — file may already be gone */ }
            }

            // Build file metadata
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name    = fileName,
                Parents = string.IsNullOrWhiteSpace(_avatarFolderId)
                    ? null
                    : new List<string> { _avatarFolderId }
            };

            // Upload
            Console.WriteLine($"[DEBUG] GoogleDriveClient: Uploading '{fileName}' to folder '{_avatarFolderId ?? "ROOT"}'...");
            var request = _service.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id, webContentLink, webViewLink";
            var result = await request.UploadAsync();

            if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                Console.WriteLine($"[DEBUG] GoogleDriveClient: Upload failed with status {result.Status}. Exception: {result.Exception?.Message}");
                if (result.Exception != null) Console.WriteLine(result.Exception.StackTrace);
                throw new InvalidOperationException(
                    $"Google Drive upload failed: {result.Exception?.Message}");
            }
            
            Console.WriteLine($"[DEBUG] GoogleDriveClient: Upload successful. File ID: {request.ResponseBody?.Id}");


            var uploadedFile = request.ResponseBody;
            var fileId       = uploadedFile.Id;

            // Make the file publicly readable (anyone with the link can view)
            await _service.Permissions.Create(
                new Permission { Type = "anyone", Role = "reader" },
                fileId
            ).ExecuteAsync();

            // Return a direct download URL that works as an <img src>
            return $"https://drive.google.com/uc?export=view&id={fileId}";
        }

        /// <summary>Extracts the Drive file ID from a previously stored Drive URL.</summary>
        public static string? ExtractFileId(string? driveUrl)
        {
            if (string.IsNullOrWhiteSpace(driveUrl)) return null;
            // Pattern: https://drive.google.com/uc?export=view&id=FILE_ID
            var uri = new Uri(driveUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["id"];
        }
    }
}
