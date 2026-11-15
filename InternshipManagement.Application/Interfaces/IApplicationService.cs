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
        Task<bool> UpdateApplicationStatusAsync(int userId, int applicationId, UpdateApplicationStatusRequest request);
        Task<bool> ShortlistApplicationAsync(int userId, int applicationId, string? notes);
        Task<bool> ScheduleInterviewAsync(int userId, int applicationId, ScheduleInterviewRequest request);
        Task<bool> RescheduleInterviewAsync(int userId, int applicationId, ScheduleInterviewRequest request);
        Task<bool> CancelInterviewAsync(int userId, int applicationId, string? reason);
        Task<bool> MarkInterviewCompletedAsync(int userId, int applicationId);
        Task<bool> MakeOfferAsync(int userId, int applicationId, MakeOfferRequest request);
        Task<bool> RespondToOfferAsync(int userId, int applicationId, bool accepted);
        Task<bool> WithdrawApplicationAsync(int userId, int applicationId);
        Task<List<ApplicationResponse>> GetShortlistedApplicationsAsync(int internshipId);
    }
}