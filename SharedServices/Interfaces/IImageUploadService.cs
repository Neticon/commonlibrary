using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IImageUploadService
    {
        Task<ServiceResponse> UploadImages(Dictionary<string, MemoryStream> files, string venueId);
        Task<ServiceResponse> GetImages(string type, string venueId);
        Task<ServiceResponse> DeleteImage(string url);
        Task<ServiceResponse> DeleteImages(List<string> urls);
    }
}
