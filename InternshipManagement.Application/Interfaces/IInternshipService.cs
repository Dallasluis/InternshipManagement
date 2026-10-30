using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IInternshipService
    {
        Task<InternshipResponse> CreateInternshipAsync(int userId, CreateInternshipRequest request);
        Task<InternshipResponse?> UpdateInternshipAsync(int userId, int internshipId, UpdateInternshipRequest request);
        Task<bool> PublishInternshipAsync(int userId, int internshipId);
        Task<bool> CloseInternshipAsync(int userId, int internshipId);
        Task<InternshipResponse?> GetInternshipByIdAsync(int id, int? requestingUserId = null);
        Task<(List<InternshipResponse> Items, int TotalCount)> SearchInternshipsAsync(InternshipSearchRequest request);
        Task<List<InternshipResponse>> GetCompanyInternshipsAsync(int companyProfileId);
        Task<bool> ModerateInternshipAsync(int internshipId, ModerationStatus status, string? notes);
        Task<bool> DeleteInternshipAsync(int userId, int internshipId);
        Task<bool> CanUserApplyAsync(int internshipId, int userId);
    }
}