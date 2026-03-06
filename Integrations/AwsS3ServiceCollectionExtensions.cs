using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken.Model;
using Amazon.SecurityToken;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class AwsS3ServiceCollectionExtensions
{
    public static IServiceCollection AddAssumedRoleS3(
        this IServiceCollection services,
        string roleArn,
        RegionEndpoint region)
    {
        services.AddSingleton<Task<IAmazonS3>>(async _ =>
        {
            Console.WriteLine("TOKEN" + Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE"));
            Console.WriteLine("ARN" + Environment.GetEnvironmentVariable("AWS_ROLE_ARN"));

            var sourceCredentials = new AssumeRoleWithWebIdentityCredentials(
                Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE"),
                Environment.GetEnvironmentVariable("AWS_ROLE_ARN"), $"IRSA-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
            );

            using var stsClient = new AmazonSecurityTokenServiceClient(sourceCredentials, RegionEndpoint.EUCentral1);

            // Check if the token works
            var identity = await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            Console.WriteLine(identity.Arn);

            //var assumedCredentials = new AssumeRoleAWSCredentials(
            //    sourceCredentials,
            //    roleArn,
            //    $"S3Session-{Environment.MachineName}"
            //);
            using var s3Client = new AmazonS3Client(sourceCredentials, RegionEndpoint.EUCentral1);
           // var buckets = await s3Client.ListBucketsAsync();
            //foreach (var b in buckets.Buckets)
            //{
            //    Console.WriteLine(b.BucketName);
            //}
            return new AmazonS3Client(sourceCredentials, region);
        });

        return services;
    }
}