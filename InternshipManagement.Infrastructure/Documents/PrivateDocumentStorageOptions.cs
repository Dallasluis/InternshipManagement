namespace InternshipManagement.Infrastructure.Documents;

public sealed class PrivateDocumentStorageOptions
{
    /// <summary>Storage backend: Local (default) or AzureBlob.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Local disk root for private documents. Relative paths are resolved from the API content root.
    /// Configure an absolute, durable path in environments where the application directory is ephemeral.
    /// </summary>
    public string RootPath { get; set; } = Path.Combine("App_Data", "PrivateDocuments");

    /// <summary>Blob service endpoint, for example https://account.blob.core.windows.net.</summary>
    public string? BlobServiceUri { get; set; }

    /// <summary>Name of the private container used for documents.</summary>
    public string ContainerName { get; set; } = "interlane-private-documents";

    /// <summary>Optional user-assigned managed identity client ID.</summary>
    public string? ManagedIdentityClientId { get; set; }
}
