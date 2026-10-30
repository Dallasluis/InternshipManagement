using InternshipManagement.Web.Models.Documents;

namespace InternshipManagement.Web.Services
{
    public interface IDocumentApiClient
    {
        Task<PrivateDocumentResponse> UploadAsync(string token, IFormFile file, string category, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PrivateDocumentResponse>> GetOwnedDocumentsAsync(string token, CancellationToken cancellationToken = default);
        Task<DocumentAccessLinkResponse?> CreateAccessLinkAsync(string token, Guid documentId, int? reportId = null, int? companyProfileId = null, CancellationToken cancellationToken = default);
        Task<DocumentFileResponse?> DownloadAsync(string token, Guid documentId, int? reportId = null, int? companyProfileId = null, CancellationToken cancellationToken = default);
        Task<bool> SetRetentionStateAsync(string token, Guid documentId, string state, CancellationToken cancellationToken = default);
    }
}
