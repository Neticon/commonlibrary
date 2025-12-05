using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IImageUploadService
    {
        Task<ServiceResponse> UploadImages(Dictionary<string, Stream> files, string venueId);
        Task<ServiceResponse> GetImages(string type);
        Task<ServiceResponse> DeleteImage(string url);
    }
}
