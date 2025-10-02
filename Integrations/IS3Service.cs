namespace CommonLibrary.Integrations
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(string bucketName, string key, string filePath, CancellationToken cancellationToken = default);
        Task<string> UploadStreamAsync(string bucketName, string key, Stream stream, string contentType = "application/octet-stream", CancellationToken cancellationToken = default);
        Task<string> DownloadFileStringAsync(string bucketName, string key, CancellationToken cancellationToken = default);
    }
}
