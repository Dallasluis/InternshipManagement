using InternshipManagement.Web.Models.Application;
using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.ViewModels
{
    public class ApplicationDetailsViewModel
    {
        public ApplicationResponse Application { get; set; } = new();
        public List<ApplicationStatusHistoryResponse> StatusHistory { get; set; } = new();
    }
}
