using Grpc.Net.Client;
using Integration.Grpc;

namespace CommonLibrary.Integrations
{
    public class MicrosoftClient : IMicrosoftClient
    {
        private readonly MicrosoftUser.MicrosoftUserClient _microsoftUserClient;
        private readonly MicrosoftEvents.MicrosoftEventsClient _microsoftEventsClient;

        private readonly string _internalKey;

        public MicrosoftClient(GrpcChannel channel, string internalKey)
        {
            _microsoftUserClient = new MicrosoftUser.MicrosoftUserClient(channel);
            _microsoftEventsClient = new MicrosoftEvents.MicrosoftEventsClient(channel);
            _internalKey = internalKey;
        }

        public async Task<MicrosoftUserResponse> CreateMicrosoftUser(MicrosoftUserRequest request)
        {
            var headers = new Grpc.Core.Metadata { { "x-internal-key", _internalKey } };
            var response = await _microsoftUserClient.CreateMicrosoftUserAsync(request, headers);
            return response;
        }

        public async Task<MicrosoftEventResponse> CreateMicrosoftEvent(MicrosoftEventRequest request)
        {
            var headers = new Grpc.Core.Metadata { { "x-internal-key", _internalKey } };
            var response = await _microsoftEventsClient.CreateEventAsync(request, headers);
            return response;
        }

        public async Task<RemoveMicrosoftUserResponse> RemoveMicrosoftUser(RemoveMicrosoftUserRequest request)
        {
            var headers = new Grpc.Core.Metadata { { "x-internal-key", _internalKey } };
            var response = await _microsoftUserClient.RemoveMicrosoftUserAsync(request, headers);
            return response;
        }
    }
}
