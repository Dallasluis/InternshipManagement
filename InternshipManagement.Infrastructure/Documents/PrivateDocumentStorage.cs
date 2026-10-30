using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace InternshipManagement.Infrastructure.Documents
{
    public sealed class PrivateDocumentStorage : IPrivateDocumentStorage
    {
        private readonly string _storageRoot;

        public PrivateDocumentStorage(IHostEnvironment environment)
        {
            _storageRoot = Path.Combine(environment.ContentRootPath, "App_Data", "PrivateDocuments");
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
