using CommonLibrary.Integrations;
using CommonLibrary.Models;
using CommonLibrary.SharedServices.Interfaces;
using ServicePortal.Application.Models;
using ServicePortal.Domain.PSQL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CommonLibrary.SharedServices.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private List<string> validTypes = ["image/jpeg", "image/png", "image/webp"];
        private readonly string _bucket = "venues-cloudfront-nonprod";
        private readonly IS3Service _s3Service;
        private readonly string CDN_URL = Environment.GetEnvironmentVariable("CDN_URL");
        private readonly string KEY_PATTERN = "{0}/{1}/{2}_{3}.webp";

        public ImageUploadService(IS3Service s3Service)
        {
            _s3Service = s3Service;
        }

        public async Task<ServiceResponse> UploadImages(Dictionary<string, Stream> files, string venueId)
        {
            //get org_code and make right key
            //determine how to get type from front - is it separate api call or should we use separate key for files
            var key = string.Format(KEY_PATTERN, "org_code", venueId, "type", DateTime.UtcNow.ToString("yyyyMMdd_HHmm"));
            var validationResponse = await ValidateImages(files);
            if (!validationResponse.success)
                return new ServiceResponse { StatusCode = 415, Result = validationResponse };
            foreach (var image in files)
            {
                await _s3Service.UploadStreamAsync(_bucket, "r/test/test.webp", image.Value, "test", 500000);
            }
            return new ServiceResponse();


        }

        public async Task<ServiceResponse> GetImages(string type)
        {
            var images = await _s3Service.ListFilesAsync(_bucket, "r/test");
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

        public static void ConvertToWebPWithCropAndResize(string inputPath, string outputPath, int targetWidth, int targetHeight)
        {
            double targetAspectRatio = (double)targetWidth / targetHeight;
            using var image = Image.Load<Rgba32>(inputPath);
            int originalWidth = image.Width;
            int originalHeight = image.Height;
            double originalAspectRatio = (double)originalWidth / originalHeight;
            Rectangle cropRectangle;
            if (originalAspectRatio > targetAspectRatio)
            {
                // Crop width
                int newWidth = (int)(originalHeight * targetAspectRatio);
                int xOffset = (originalWidth - newWidth) / 2;
                cropRectangle = new Rectangle(xOffset, 0, newWidth, originalHeight);
            }
            else
            {
                // Crop height
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
                Quality = 75,
                FileFormat = WebpFileFormatType.Lossy
            };
            image.Save(outputPath, encoder);
        }
    }
}
