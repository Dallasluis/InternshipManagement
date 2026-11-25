using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InternshipManagement.Application.Interfaces;

namespace InternshipManagement.Infrastructure.Documents;

/// <summary>Stores private documents in an Azure Blob container with public access disabled.</summary>
public sealed class AzureBlobPrivateDocumentStorage : IPrivateDocumentStorage
{
    private readonly BlobContainerClient _container;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public AzureBlobPrivateDocumentStorage(BlobServiceClient serviceClient, string containerName)
    {
        _container = serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task SaveAsync(Guid documentId, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        await _container.GetBlobClient(GetBlobName(documentId))
            .UploadAsync(content, overwrite: false, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        var response = await _container.GetBlobClient(GetBlobName(documentId))
            .DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        await _container.GetBlobClient(GetBlobName(documentId))
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            await _container.CreateIfNotExistsAsync(
                publicAccessType: PublicAccessType.None,
                cancellationToken: cancellationToken);

            var properties = await _container.GetPropertiesAsync(cancellationToken: cancellationToken);
            if (properties.Value.PublicAccess is not null)
                throw new InvalidOperationException(
                    $"Blob container '{_container.Name}' allows public access. Private document storage requires public access to be disabled.");

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static string GetBlobName(Guid documentId) => $"{documentId:N}.bin";
}
