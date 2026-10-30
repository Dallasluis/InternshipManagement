using InternshipManagement.Application.DTOs.Reports;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IReportService
    {
        Task<InternshipReportResponse?> SubmitInternshipReportAsync(int reporterId, SubmitInternshipReportRequest request);
    }
}
