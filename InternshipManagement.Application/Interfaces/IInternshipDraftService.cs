using InternshipManagement.Application.DTOs.Internship;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IInternshipDraftService
    {
        /// <summary>Generates a draft from a plain-language description and saves it for the company.</summary>
        Task<InternshipDraftResponse> GenerateDraftAsync(
            int userId, GenerateInternshipDraftRequest request, CancellationToken cancellationToken = default);

        Task<List<InternshipDraftSummary>> ListDraftsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>Null when the draft doesn't exist or belongs to another company.</summary>
        Task<InternshipDraftResponse?> GetDraftAsync(int userId, int draftId, CancellationToken cancellationToken = default);

        /// <summary>Null when the draft doesn't exist or belongs to another company.</summary>
        Task<InternshipDraftResponse?> UpdateDraftAsync(
            int userId, int draftId, SaveInternshipDraftRequest request, CancellationToken cancellationToken = default);

        /// <summary>False when the draft doesn't exist or belongs to another company.</summary>
        Task<bool> DeleteDraftAsync(int userId, int draftId, CancellationToken cancellationToken = default);
    }
}
