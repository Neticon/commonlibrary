using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace CommonLibrary.Integrations
{
    public static class GrpcSevicesExtension
    {
        public static IServiceCollection AddGrpcClient(this IServiceCollection services, string baseUrl, string internalKey)
        {
            if (!string.IsNullOrEmpty(baseUrl))
            {
                services.AddSingleton(sp =>
                {
                    var channel = GrpcChannel.ForAddress(baseUrl);
                    return new EmailClient(channel, internalKey);
                });
            }

            services.AddSingleton<IEmailClient>(sp => sp.GetRequiredService<EmailClient>());

            return services;
        }
    }
}
