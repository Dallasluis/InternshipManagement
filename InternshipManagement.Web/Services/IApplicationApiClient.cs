using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.Services
{
    public interface IApplicationApiClient
    {
        Task<ApplicationResponse?> ApplyAsync(string token, int studentId, ApplyRequest request);
        Task<List<ApplicationResponse>> GetStudentApplicationsAsync(string token, int studentId);
        Task<List<ApplicationResponse>> GetInternshipApplicationsAsync(string token, int internshipId);
        Task<ApplicationResponse?> GetApplicationByIdAsync(string token, int id);
        Task<bool> UpdateStatusAsync(string token, int id, UpdateApplicationStatusRequest request);
        Task<bool> ShortlistAsync(string token, int id, string? notes);
        Task<bool> WithdrawAsync(string token, int id);
        Task<List<ApplicationResponse>> GetShortlistedAsync(string token, int internshipId);
        Task<StudentStatsResponse?> GetStudentStatsAsync(string token, int studentId);
        Task<CompanyStatsResponse?> GetCompanyStatsAsync(string token, int userId);
        Task<List<ApplicationResponse>> GetCompanyApplicationsAsync(string token, int userId);
    }

    public class StudentStatsResponse
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Shortlisted { get; set; }
        public int Rejected { get; set; }
    }

    public class CompanyStatsResponse
    {
        public int TotalInternships { get; set; }
        public int ActiveInternships { get; set; }
        public int TotalApplications { get; set; }
        public int ShortlistedCount { get; set; }
    }
}