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

namespace CommonLibrary.SharedServices.Services
{
    public class ImageUploadService : AppServiceBase, IImageUploadService
    {
        private List<string> validTypes = ["image/jpeg", "image/png", "image/webp"];
        private readonly string _bucket = "venues-cloudfront-nonprod";
        private readonly IS3Service _s3Service;
        private readonly string CDN_URL = Environment.GetEnvironmentVariable("CDN_URL");
        private readonly string KEY_PATTERN = "r/{0}/{1}/{2}_{3}.webp";
        private readonly string KEY_PATTERN_ARRAY = "r/{0}/{1}/{2}_{3}_{4}.webp";

        public ImageUploadService(IS3Service s3Service, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _s3Service = s3Service;
        }

        public async Task<ServiceResponse> UploadImages(Dictionary<string, Stream> files, string venueId)
        {
            var validationResponse = await ValidateImages(files);
            if (!validationResponse.success)
                return new ServiceResponse { StatusCode = 415, Result = validationResponse };
            var response = new UploadResponse();
            foreach (var image in files)
            {
                var type = image.Key.Split('_')[0];
                var index = image.Key.Split('_')[1];
                var key = "";
                var width = 0;
                var height = 0;
                if(files.Count > 1)
                {
                     key = string.Format(KEY_PATTERN_ARRAY, CurrentUser.OrgCode, venueId, type, DateTime.UtcNow.ToString("yyyyMMdd_HHmm"), index); 
                }
                else
                {
                     key = string.Format(KEY_PATTERN, CurrentUser.OrgCode, venueId, type, DateTime.UtcNow.ToString("yyyyMMdd_HHmm"));
                }
                
                if(type == "logo")
                {
                    if (response.logo == null)
                        response.logo = new List<string>();
                    response.logo.Add($"{CDN_URL}{key}");
                    width = 60;
                    height = 60;
                }else if (type == "cover")
                {
                    if (response.cover == null)
                        response.cover = new List<string>();
                    response.cover.Add($"{CDN_URL}{key}");
                    width = 400;
                    height = 300;
                }
                var resizedStream = ConvertToWebPWithCropAndResize(image.Value, width, height);
                await _s3Service.UploadStreamAsync(_bucket, key, resizedStream, "Service_Portal", null);
            }
            var graphApiResponse = new GraphAPIResponse<UploadResponse> { rows = new List<UploadResponse> { response }, success = true, request_id = Guid.NewGuid() };
            return new ServiceResponse { Result = graphApiResponse};
        }

        public async Task<ServiceResponse> GetImages(string type, string venueId)
        {
            var images = await _s3Service.ListFilesAsync(_bucket, $"r/{CurrentUser.OrgCode}/{venueId}");
            images.ForEach(image => { image.Key = $"{CDN_URL}{image.Key}"; });
            var response = new GraphAPIResponse<S3FileMetadata>()
            {
                success = true,
                request_id = Guid.NewGuid(),
                rows = images,
            };

            return new ServiceResponse { Result = response, StatusCode = 200 };
        }

        public async Task<ServiceResponse> DeleteImage(string url)
        {
            var key = url.Replace(CDN_URL, "");
            await _s3Service.DeleteFileAsync(_bucket, key);
            return new ServiceResponse { Result = url, StatusCode = 200 };
        }


        private async Task<GraphAPIResponse<object>> ValidateImages(Dictionary<string, Stream> images)
        {
            var stage = "upload_validation";
            var valid = true;
            var invalidKeys = new List<string>();
            var operation = "stream_validation";
            var message = "Uploaded file is not an image.";
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
                message = "Unsupported image type, only 'image/jpeg', 'image/png', and 'image/webp' are supported.";
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
        }
    }
}
