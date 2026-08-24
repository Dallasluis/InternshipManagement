using InternshipManagement.Web.Models.Internship;

namespace InternshipManagement.Web.Services
{
    public interface IInternshipApiClient
    {
        Task<PagedInternshipResult> SearchAsync(InternshipSearchViewModel filters);
        Task<InternshipResponse?> GetByIdAsync(int id);
        Task<List<InternshipResponse>> GetCompanyInternshipsAsync(string token, int userId);
        Task<InternshipResponse?> CreateInternshipAsync(string token, int userId, CreateInternshipRequest request);
        Task<InternshipResponse?> UpdateInternshipAsync(string token, int id, UpdateInternshipRequest request);
        Task<bool> PublishInternshipAsync(string token, int id);
        Task<bool> CloseInternshipAsync(string token, int id);
        Task<bool> DeleteInternshipAsync(string token, int id);
    }
}