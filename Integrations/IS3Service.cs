using ServicePortal.Application.Models;

namespace CommonLibrary.Integrations
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(string bucketName, string key, string filePath, CancellationToken cancellationToken = default);
        Task<string> UploadStreamAsync(string bucketName, string key, Stream stream, string uploadedBy, double? expireSeconds, string contentType = "application/octet-stream", CancellationToken cancellationToken = default);
        Task<string> DownloadFileStringAsync(string bucketName, string key, CancellationToken cancellationToken = default);
        Task<List<S3FileMetadata>> ListFilesAsync(string bucketName, string prefix, string fileExtension = ".webp", string? fileNameStartsWith = null);
        Task DeleteFileAsync(string bucket, string key);
    }
}
