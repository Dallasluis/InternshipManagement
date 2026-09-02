using InternshipManagement.Application.DTOs.Application;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IApplicationService
    {
        Task<ApplicationResponse> ApplyAsync(int studentId, ApplyRequest request);
        Task<ApplicationResponse> GetApplicationByIdAsync(int applicationId);
        Task<List<ApplicationResponse>> GetStudentApplicationsAsync(int studentId);
        Task<List<ApplicationResponse>> GetInternshipApplicationsAsync(int internshipId);
        Task<bool> UpdateApplicationStatusAsync(int applicationId, UpdateApplicationStatusRequest request);
        Task<bool> ShortlistApplicationAsync(int applicationId, string? notes);
        Task<bool> ScheduleInterviewAsync(int applicationId, ScheduleInterviewRequest request);
        Task<bool> MarkInterviewCompletedAsync(int applicationId);
        Task<bool> MakeOfferAsync(int applicationId, MakeOfferRequest request);
        Task<bool> RespondToOfferAsync(int applicationId, bool accepted);
        Task<bool> WithdrawApplicationAsync(int applicationId);
        Task<List<ApplicationResponse>> GetShortlistedApplicationsAsync(int internshipId);
    }
}