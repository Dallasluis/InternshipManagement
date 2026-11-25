using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternshipManagement.Infrastructure.Documents;

public static class PrivateDocumentStorageRegistrationExtensions
{
    public static IServiceCollection AddPrivateDocumentStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PrivateDocumentStorageOptions>()
            .Bind(configuration.GetSection("PrivateDocumentStorage"))
            .Validate(options => options.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase)
                || options.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase),
                "PrivateDocumentStorage:Provider must be Local or AzureBlob.")
            .Validate(options => !options.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase)
                || (Uri.TryCreate(options.BlobServiceUri, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)
                    && !string.IsNullOrWhiteSpace(options.ContainerName)),
                "AzureBlob requires a valid HTTPS BlobServiceUri (HTTP is allowed for local emulators) and a container name.")
            .ValidateOnStart();

        services.AddSingleton<IPrivateDocumentStorage>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<PrivateDocumentStorageOptions>>().Value;
            if (options.Provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
            {
                var credentialOptions = new DefaultAzureCredentialOptions();
                if (!string.IsNullOrWhiteSpace(options.ManagedIdentityClientId))
                    credentialOptions.ManagedIdentityClientId = options.ManagedIdentityClientId;

                var credential = new DefaultAzureCredential(credentialOptions);
                var serviceClient = new BlobServiceClient(new Uri(options.BlobServiceUri!), credential);
                return new AzureBlobPrivateDocumentStorage(serviceClient, options.ContainerName);
            }

            return new PrivateDocumentStorage(
                provider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
                provider.GetRequiredService<IOptions<PrivateDocumentStorageOptions>>());
        });

        return services;
    }
}
