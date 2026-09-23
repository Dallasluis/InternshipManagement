using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.ViewModels
{
    public class PlacementDetailsViewModel
    {
        public PlacementResponse Placement { get; set; } = new();
        public List<ProgressReportResponse> ProgressReports { get; set; } = new();
        public List<EvaluationResponse> Evaluations { get; set; } = new();
        public bool IsCompanyView { get; set; }
    }
}
