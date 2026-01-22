namespace CommonLibrary.Integrations;

using Grpc.Net.Client;
using Integration.Grpc; // from option csharp_namespace

public class EmailClient : IEmailClient
{
    private readonly Email.EmailClient _emailClient;
    private readonly string _internalKey;

    public EmailClient(GrpcChannel channel, string internalKey)
    {
        _emailClient = new Email.EmailClient(channel);
        _internalKey = internalKey;
    }

    public async Task<string> SendEmailAsync(SendEmailRequest request)
    {
        var headers = new Grpc.Core.Metadata { { "x-internal-key", _internalKey } };
        var response = await _emailClient.SendEmailAsync(request, headers);
        return response.Response;
    }
}

