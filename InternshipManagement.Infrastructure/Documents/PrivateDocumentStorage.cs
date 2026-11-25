using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace InternshipManagement.Infrastructure.Documents
{
    public sealed class PrivateDocumentStorage : IPrivateDocumentStorage
    {
        private readonly string _storageRoot;

        public PrivateDocumentStorage(IWebHostEnvironment environment, IOptions<PrivateDocumentStorageOptions> options)
        {
            var configuredPath = options.Value.RootPath;
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("PrivateDocumentStorage:RootPath must not be empty.");

            _storageRoot = Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

            if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
            {
                var webRoot = Path.GetFullPath(environment.WebRootPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var webRootPrefix = webRoot + Path.DirectorySeparatorChar;
                if (string.Equals(_storageRoot, webRoot, StringComparison.OrdinalIgnoreCase)
                    || _storageRoot.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Private document storage must be outside the public web root.");
                }
            }
        }

        public async Task SaveAsync(Guid documentId, Stream content, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_storageRoot);
            var path = GetPath(documentId);
            await using var output = new FileStream(
                path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await content.CopyToAsync(output, cancellationToken);
        }

        public Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stream stream = new FileStream(
                GetPath(documentId), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = GetPath(documentId);
            if (File.Exists(path))
                File.Delete(path);
            return Task.CompletedTask;
        }

        private string GetPath(Guid documentId) =>
            Path.Combine(_storageRoot, $"{documentId:N}.bin");
    }
}
