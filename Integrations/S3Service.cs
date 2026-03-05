using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using ServicePortal.Application.Models;
using System.Text;

namespace CommonLibrary.Integrations
{
    public class S3Service : IS3Service
    {
        private IAmazonS3 _s3Client;

        public S3Service(IAmazonS3 s3Service)
        {
            _s3Client = s3Service;
        }

        public static async Task LogCurrentRoleAsync()
        {
            try
            {
                using var stsClient = new AmazonSecurityTokenServiceClient(); // auto picks up default credentials
                var response = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());

                Console.WriteLine($"Account: {response.Account}");
                Console.WriteLine($"UserId: {response.UserId}");
                Console.WriteLine($"Arn: {response.Arn}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task<string> UploadFileAsync(string bucketName, string key, string filePath, CancellationToken cancellationToken = default)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath,
                ContentType = GetContentType(filePath),
                CannedACL = S3CannedACL.PublicRead // 👈 makes file public
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            return GetPublicUrl(bucketName, key);
        }

        public async Task<string> UploadStreamAsync(string bucketName, string key, Stream stream, string uploadedBy, double? expireSeconds, bool noCache = true, bool cachePublic = false, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
        {
            var cacheControl = "";
            if (cachePublic)
                cacheControl = cacheControl + "public,";
            if (noCache)
                cacheControl = cacheControl + "no-cache,";
            if (expireSeconds.HasValue)
            {
                cacheControl = cacheControl + $"max-age={expireSeconds}";
            }
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                Metadata =
                {
                    ["x-amz-meta-uploaded-by"] = uploadedBy
                },
                Headers =
                {
                    ContentType = contentType,
                    Expires = expireSeconds != null ? DateTime.UtcNow.AddSeconds(expireSeconds.Value) : null,
                    CacheControl = cacheControl.TrimEnd(',')
                }
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            return GetPublicUrl(bucketName, key);
        }

        public async Task<Stream> DownloadFileAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(request, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0; // reset to beginning

            return memoryStream;
        }

        public async Task<string> DownloadFileStringAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {

            var stream = await DownloadFileAsync(bucketName, key, cancellationToken);
            return await StreamToStringAsync(stream);
        }

        public static async Task<string> StreamToStringAsync(Stream stream, Encoding? encoding = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            encoding ??= Encoding.UTF8; // default to UTF-8

            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            stream.Position = 0; // reset to beginning
            return await reader.ReadToEndAsync();
        }

        public async Task<List<S3FileMetadata>> ListFilesAsync(
        string bucketName, string prefix,
        string fileExtension = ".webp",
        string? fileNameStartsWith = null)
        {
            var files = new List<S3FileMetadata>();
            string continuationToken = null;
            do
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix.EndsWith("/") ? prefix : prefix + "/",
                    ContinuationToken = continuationToken
                };
                var response = await _s3Client.ListObjectsV2Async(request);
                if (response.S3Objects != null)
                {
                    var filtered = response.S3Objects
                        .Where(o =>
                            o.Key.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase) &&
                            (fileNameStartsWith == null ||
                             Path.GetFileName(o.Key).StartsWith(fileNameStartsWith, StringComparison.OrdinalIgnoreCase)))
                        .Select(o => new S3FileMetadata
                        {
                            Key = o.Key,
                            Size = o.Size,
                            LastModified = o.LastModified
                        });
                    files.AddRange(filtered);
                }
                continuationToken = response.IsTruncated.Value ? response.NextContinuationToken : null;
            } while (continuationToken != null);
            return files;
        }

        public async Task DeleteFileAsync(string bucket, string key)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key // e.g., "images/output.webp"
            };
            var response = await _s3Client.DeleteObjectAsync(request);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception($"Failed to delete '{key}' from bucket '{bucket}'");
            }
        }

        public async Task<DeleteObjectsResponse> DeleteFilesAsync(string bucket, List<string> keys)
        {
            var keysToRemove = keys.Select(q => new KeyVersion { Key = q }).ToList();
            var request = new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = keysToRemove
            };
            var response = await _s3Client.DeleteObjectsAsync(request);
            return response;
        }

        private static string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".js" => "application/javascript",
                _ => "application/octet-stream"
            };
        }

        private string GetPublicUrl(string bucketName, string key)
        {
            return $"https://{bucketName}.s3.{_s3Client.Config.RegionEndpoint.SystemName}.amazonaws.com/{key}";
        }
    }
}
