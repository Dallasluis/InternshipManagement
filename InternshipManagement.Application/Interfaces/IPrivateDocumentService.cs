using InternshipManagement.Application.DTOs.Documents;

namespace InternshipManagement.Application.Interfaces
{
    public interface IPrivateDocumentService
    {
        Task<PrivateDocumentResponse> UploadAsync(
            int userId,
            string role,
            string category,
            string fileName,
            long length,
            Stream content,
            CancellationToken cancellationToken);
        Task<IReadOnlyList<PrivateDocumentResponse>> GetOwnedDocumentsAsync(int userId, CancellationToken cancellationToken);
        Task<DocumentAccessLinkResponse?> CreateAccessLinkAsync(
            Guid documentId,
            int userId,
            string role,
            int? reportId,
            int? companyProfileId,
            string accessPath,
            CancellationToken cancellationToken);
        Task<(PrivateDocumentResponse Document, Stream Content)?> OpenAccessLinkAsync(
            string token,
            int userId,
            string role,
            CancellationToken cancellationToken);
        Task<bool> SetRetentionStateAsync(
            Guid documentId,
            int userId,
            string role,
            string state,
            CancellationToken cancellationToken);
    }
}
