using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Models;
using CommonLibrary.SharedServices.Interfaces;
using ServicePortal.Application.Interfaces;
using ServicePortal.Application.Models;
using ServicePortal.Domain.PSQL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;

namespace CommonLibrary.SharedServices.Services
{
    public class ImageUploadService : AppServiceBase, IImageUploadService
    {
        private List<string> validTypes = ["image/jpeg", "image/png", "image/webp"];
        private readonly string _bucket = "venues-cloudfront-nonprod";
        private readonly IS3Service _s3Service;
        private readonly string CDN_URL = Environment.GetEnvironmentVariable("CDN_URL");
        private readonly string KEY_PATTERN = "r/{0}/{1}/{2}/{3}_{4}.webp";
        private readonly string KEY_PATTERN_ARRAY = "r/{0}/{1}/{2}/{3}_{4}_{5}.webp";
        private readonly string KEY_PATTERN_WITH_FILENAME = "r/{0}/{1}/{2}/{3}";
        private readonly string FILENAME_PATTERN = @"^.+_\d{8}_([01]\d|2[0-3])[0-5]\d[0-5]\d_(10|[1-9])\..+$";
        private readonly int IMAGE_LIMIT_BY_TYPE = 10;
 
        public ImageUploadService(IS3Service s3Service, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _s3Service = s3Service;
        }

        public async Task<ServiceResponse> UploadImages(Dictionary<string, MemoryStream> files, string venueId)
        {
            if (CurrentUser.Role.Equals("ASSISTANT", StringComparison.OrdinalIgnoreCase))
            {
                if (CurrentUser.Venues != null && !CurrentUser.Venues.Contains(venueId))
                {
                    return new ServiceResponse { StatusCode = 403, Result = "ui_upload_unauthorized - Unauthorized request." };
                }
            }
            //check images count on S3 (currently only one type is expected in request)
            var imgType = files.First().Key.Split('_')[0];
            var imagesCount = await GetImageCountByType(venueId, CurrentUser.OrgCode, imgType);
            if (imagesCount + files.Count > IMAGE_LIMIT_BY_TYPE)
                return new ServiceResponse { StatusCode = 409, Result = "ui_upload_limit" };

            //validate type
            var validationResponse = await ValidateImages(files);
            if (!validationResponse.success)
                return new ServiceResponse { StatusCode = 415, Result = validationResponse };

            //validate filename
            var validatonFilenameResponse = await ValidateFilename(files);
            if (!validatonFilenameResponse.success)
                return new ServiceResponse { StatusCode = 415, Result = validatonFilenameResponse };

            var response = new UploadResponse();
            foreach (var image in files)
            {
                var keyParts = image.Key.Split("_");
                var type = keyParts[0];
                var filename = string.Join("_", keyParts.Skip(2));
                var key = "";
                var width = 0;
                var height = 0;
  
                var filenameWithNewExtension = Path.ChangeExtension(filename, ".webp");
                key = string.Format(KEY_PATTERN_WITH_FILENAME, CurrentUser.OrgCode, venueId, type, filenameWithNewExtension);

                if (type == ImageUploadTypes.Logo)
                {
                    if (response.logo == null)
                        response.logo = new List<string>();
                    response.logo.Add($"{CDN_URL}{key}");
                    width = 60;
                    height = 60;
                } else if (type == ImageUploadTypes.Cover)
                {
                    if (response.cover == null)
                        response.cover = new List<string>();
                    response.cover.Add($"{CDN_URL}{key}");
                    width = 400;
                    height = 300;
                }
                else if (type == ImageUploadTypes.Service)
                {
                    if (response.service == null)
                        response.service = new List<string>();
                    response.service.Add($"{CDN_URL}{key}");
                    width = 400;
                    height = 300;
                }
                var resizedStream = ConvertToWebPWithCropAndResize(image.Value, width, height);
                await _s3Service.UploadStreamAsync(_bucket, key, resizedStream, "Service_Portal", null);
            }
            var graphApiResponse = new GraphAPIResponse<UploadResponse> { rows = new List<UploadResponse> { response }, success = true, request_id = Guid.NewGuid() };
            return new ServiceResponse { Result = graphApiResponse };
        }

        public async Task<ServiceResponse> GetImages(string type, string venueId)
        {
            var images = await _s3Service.ListFilesAsync(_bucket, $"r/{CurrentUser.OrgCode}/{venueId}/{type}");
            images.ForEach(image => { image.Key = $"{CDN_URL}{image.Key}"; });
            var response = new GraphAPIResponse<S3FileMetadata>()
            {
                success = true,
                request_id = Guid.NewGuid(),
                rows = images,
            };

            return new ServiceResponse { Result = response, StatusCode = 200 };
        }

        private async Task<CountByType> GetImageCountByType(string venueId) // unused for now, if UI changes to support multiple types we will use this (not to check all types)
        {
            var imagesLogo = await _s3Service.ListFilesAsync(_bucket, $"r/{CurrentUser.OrgCode}/{venueId}/{ImageUploadTypes.Logo}");
            var imagesCover = await _s3Service.ListFilesAsync(_bucket, $"r/{CurrentUser.OrgCode}/{venueId}/{ImageUploadTypes.Cover}");
            var imagesService = await _s3Service.ListFilesAsync(_bucket, $"r/{CurrentUser.OrgCode}/{venueId}/{ImageUploadTypes.Service}");

            return new CountByType
            {
                cover = imagesCover.Count(),
                logo = imagesLogo.Count(),
                service = imagesService.Count(),
            };
        }

        private async Task<int> GetImageCountByType(string venueId, string orgCode, string type) // unused for now, if UI changes to support multiple types we will use this (not to check all types)
        {
            var images = await _s3Service.ListFilesAsync(_bucket, $"r/{orgCode}/{venueId}/{type}", ".jpg");
            return images.Count();
        }

        public async Task<ServiceResponse> DeleteImage(string url)
        {
            var key = url.Replace(CDN_URL, "");
            await _s3Service.DeleteFileAsync(_bucket, key);
            return new ServiceResponse { Result = url, StatusCode = 200 };
        }

        public async Task<ServiceResponse> DeleteImages(List<string> urls)
        {
            var keys = urls.Select(q => q.Replace(CDN_URL, "")).ToList();
            var s3Result = await _s3Service.DeleteFilesAsync(_bucket, keys);
            if(s3Result.DeleteErrors!= null && s3Result.DeleteErrors.Count > 0)
            {
                return new ServiceResponse { Result = s3Result.DeleteErrors, StatusCode = (int)s3Result.HttpStatusCode };
            }
            return new ServiceResponse { Result = urls, StatusCode = (int) s3Result.HttpStatusCode};
        }


        private async Task<GraphAPIResponse<object>> ValidateImages(Dictionary<string, MemoryStream> images)
        {
            var stage = "upload_validation";
            var valid = true;
            var invalidKeys = new List<string>();
            var operation = "stream_validation";
            var message = "ui_upload_filemime";
            //stage1 - check is stram an image
            foreach (var image in images)
            {
                try
                {
                    using var img = Image.Load(image.Value); // Throws if not a valid image
                }
                catch (Exception)
                {
                    invalidKeys.Add(image.Key);
                    valid = false;
                }
            }
            //stage2 - check type
            if (valid)
            {
                operation = "image_validation";
                message = "ui_upload_unsupported";
                foreach (var img in images)
                {
                    img.Value.Position = 0; 
                    var mimeType = HeyRed.Mime.MimeGuesser.GuessMimeType(img.Value);
                    if (!validTypes.Contains(mimeType))
                    {
                        invalidKeys.Add(img.Key);
                        valid = false;
                    }
                }
            }
            if (valid)
                return new GraphAPIResponse<object>() { success = true };
            else
                return new GraphAPIResponse<object>() { success = false, stage = stage, message = message, operation = operation, invalid_keys = invalidKeys };

        }

        private async Task<GraphAPIResponse<object>> ValidateFilename(Dictionary<string, MemoryStream> images)
        {
            var stage = "upload_validation";
            var invalidKeys = new List<string>();
            var operation = "filename_validation";
            var message = "ui_upload_filename.";
            //stage1 - check is stram an image
            foreach (var image in images)
            {
                var valid = true;

                var keyParts = image.Key.Split("_");
                var type = keyParts[0];
                var filename = string.Join("_", keyParts.Skip(2));

                if (type == ImageUploadTypes.Service && !filename.StartsWith("SRV"))
                    valid = false;
                else if (type != ImageUploadTypes.Service && !filename.StartsWith(type))
                    valid = false;

                if(valid)
                    valid = Regex.IsMatch(filename, FILENAME_PATTERN);

                if (!valid)
                    invalidKeys.Add(filename);
            }
            if (invalidKeys.Count == 0)
                return new GraphAPIResponse<object>() { success = true };
            else
                return new GraphAPIResponse<object>() { success = false, stage = stage, message = message, operation = operation, invalid_keys = invalidKeys };

        }

        public static MemoryStream ConvertToWebPWithCropAndResize(
            Stream inputStream,
            int targetWidth,
            int targetHeight)
        {
            inputStream.Position = 0;

            double targetAspectRatio = (double)targetWidth / targetHeight;

            using var image = Image.Load<Rgba32>(inputStream);

            int originalWidth = image.Width;
            int originalHeight = image.Height;

            double originalAspectRatio = (double)originalWidth / originalHeight;

            Rectangle cropRectangle;

            if (originalAspectRatio > targetAspectRatio)
            {
                int newWidth = (int)(originalHeight * targetAspectRatio);
                int xOffset = (originalWidth - newWidth) / 2;
                cropRectangle = new Rectangle(xOffset, 0, newWidth, originalHeight);
            }
            else
            {
                int newHeight = (int)(originalWidth / targetAspectRatio);
                int yOffset = (originalHeight - newHeight) / 2;
                cropRectangle = new Rectangle(0, yOffset, originalWidth, newHeight);
            }

            image.Mutate(x => x
                .Crop(cropRectangle)
                .Resize(targetWidth, targetHeight)
            );

            image.Metadata.HorizontalResolution = 72;
            image.Metadata.VerticalResolution = 72;

            var encoder = new WebpEncoder
            {
                Quality = 90,
                FileFormat = WebpFileFormatType.Lossy,
                Method = WebpEncodingMethod.BestQuality
            };

            var outputStream = new MemoryStream();

            image.Save(outputStream, encoder);

            outputStream.Position = 0; // VERY IMPORTANT

            return outputStream;
        }

        private class UploadResponse
        {
            public List<string> logo { get; set; }
            public List<string> cover { get; set; }
            public List<string> service { get; set; }
        }

        private class CountByType
        {
            public int logo { get; set; }
            public int cover { get; set; }
            public int service { get; set; }    
        }
    }
}
