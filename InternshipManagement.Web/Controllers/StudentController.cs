using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using InternshipManagement.Web.Models.Student;
using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentApiClient _studentApiClient;
        private readonly IApplicationApiClient _applicationApiClient;

        public StudentController(
            IStudentApiClient studentApiClient,
            IApplicationApiClient applicationApiClient)
        {
            _studentApiClient = studentApiClient;
            _applicationApiClient = applicationApiClient;
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var profile = await _studentApiClient.GetProfileAsync(token, int.Parse(userId));
            var applications = await _applicationApiClient.GetStudentApplicationsAsync(token, int.Parse(userId));
            var stats = await _applicationApiClient.GetStudentStatsAsync(token, int.Parse(userId));

            var viewModel = new StudentDashboardViewModel
            {
                Profile = profile,
                RecentApplications = applications?.Take(5).ToList() ?? new List<ApplicationResponse>(),
                TotalApplications = stats?.Total ?? 0,
                PendingApplications = stats?.Pending ?? 0,
                ShortlistedCount = stats?.Shortlisted ?? 0,
                RejectedCount = stats?.Rejected ?? 0,
                ProfileCompletion = CalculateProfileCompletion(profile)
            };

            return View(viewModel);
        }

        private int CalculateProfileCompletion(StudentProfileResponse? profile)
        {
            if (profile == null) return 0;
            int completion = 0;
            if (!string.IsNullOrEmpty(profile.Bio)) completion += 10;
            if (!string.IsNullOrEmpty(profile.Location)) completion += 10;
            if (!string.IsNullOrEmpty(profile.PhoneNumber)) completion += 5;
            if (!string.IsNullOrEmpty(profile.LinkedInUrl)) completion += 5;
            if (!string.IsNullOrEmpty(profile.PortfolioUrl)) completion += 5;
            if (!string.IsNullOrEmpty(profile.University)) completion += 10;
            if (!string.IsNullOrEmpty(profile.Programme)) completion += 10;
            if (!string.IsNullOrEmpty(profile.YearOfStudy)) completion += 5;
            if (!string.IsNullOrEmpty(profile.ResumeUrl)) completion += 15;
            if (profile.Education?.Any() == true) completion += 10;
            if (profile.WorkExperience?.Any() == true) completion += 10;
            if (profile.Skills?.Any() == true) completion += 5;
            return completion;
        }
    }
}