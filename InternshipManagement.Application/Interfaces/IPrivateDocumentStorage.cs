namespace InternshipManagement.Application.Interfaces
{
    public interface IPrivateDocumentStorage
    {
        Task SaveAsync(Guid documentId, Stream content, CancellationToken cancellationToken);
        Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken);
        Task DeleteAsync(Guid documentId, CancellationToken cancellationToken);
    }
}
