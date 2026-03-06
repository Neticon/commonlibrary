using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

public static class AwsS3ServiceCollectionExtensions
{
    public static IServiceCollection AddAssumedRoleS3(
        this IServiceCollection services,
        string roleArn,
        RegionEndpoint region)
    {
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var sourceCredentials = FallbackCredentialsFactory.GetCredentials(); // <- will return AssumeRoleWithWebIdentityCredentials

            var assumedCredentials = new AssumeRoleAWSCredentials(
                sourceCredentials,
                roleArn,
                $"S3Session-{Environment.MachineName}"
            );

            return new AmazonS3Client(assumedCredentials, region);
        });

        return services;
    }
}