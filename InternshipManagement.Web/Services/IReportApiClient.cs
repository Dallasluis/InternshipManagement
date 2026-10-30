using InternshipManagement.Web.Models.Internship;

namespace InternshipManagement.Web.Services
{
    public interface IReportApiClient
    {
        Task SubmitInternshipReportAsync(string token, SubmitInternshipReportRequest request);
    }
}
