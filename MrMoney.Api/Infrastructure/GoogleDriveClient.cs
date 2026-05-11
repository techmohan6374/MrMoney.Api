using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Text.Json;

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

        public GoogleDriveClient(IConfiguration configuration)
        {
            _avatarFolderId = configuration["GoogleDrive:AvatarFolderId"] ?? string.Empty;

            var projectId = configuration["GoogleServiceAccount:ProjectId"];
            var privateKeyId = configuration["GoogleServiceAccount:PrivateKeyId"];
            var privateKey = configuration["GoogleServiceAccount:PrivateKey"];
            privateKey = privateKey?.Replace("\\n", "\n");
            var clientEmail = configuration["GoogleServiceAccount:ClientEmail"];
            var clientId = configuration["GoogleServiceAccount:ClientId"];

            privateKey = privateKey.Replace("\\n", "\n");

            var credentialObject = new
            {
                type = "service_account",
                project_id = projectId,
                private_key_id = privateKeyId,
                private_key = privateKey.Replace("\\n", "\n"),
                client_email = clientEmail,
                client_id = clientId,
                auth_uri = "https://accounts.google.com/o/oauth2/auth",
                token_uri = "https://oauth2.googleapis.com/token",
                auth_provider_x509_cert_url = "https://www.googleapis.com/oauth2/v1/certs",
                client_x509_cert_url = $"https://www.googleapis.com/robot/v1/metadata/x509/{Uri.EscapeDataString(clientEmail)}"
            };

            var json = JsonSerializer.Serialize(credentialObject);

            var credential = GoogleCredential
                .FromJson(json)
                .CreateScoped(SheetsService.Scope.Spreadsheets);

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MrMoney"
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
