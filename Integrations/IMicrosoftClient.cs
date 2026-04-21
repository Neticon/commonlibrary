using Integration.Grpc;
using System.Threading.Tasks;

namespace CommonLibrary.Integrations
{
    public interface IMicrosoftClient
    {
        Task<MicrosoftUserResponse> CreateMicrosoftUser(MicrosoftUserRequest request);
        Task<RemoveMicrosoftUserResponse> RemoveMicrosoftUser(RemoveMicrosoftUserRequest request);

        Task<MicrosoftEventResponse> CreateMicrosoftEvent(MicrosoftEventRequest request);
    }
}
