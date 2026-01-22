using Integration.Grpc;

namespace CommonLibrary.Integrations
{
    public interface IEmailClient
    {
        Task<string> SendEmailAsync(SendEmailRequest request);
    }
}
