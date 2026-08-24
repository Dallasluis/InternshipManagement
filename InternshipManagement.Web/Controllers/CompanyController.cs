using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using InternshipManagement.Web.Models.Internship;
using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ICompanyApiClient _companyApiClient;
        private readonly IInternshipApiClient _internshipApiClient;
        private readonly IApplicationApiClient _applicationApiClient;

        public CompanyController(
            ICompanyApiClient companyApiClient,
            IInternshipApiClient internshipApiClient,
            IApplicationApiClient applicationApiClient)
        {
            _companyApiClient = companyApiClient;
            _internshipApiClient = internshipApiClient;
            _applicationApiClient = applicationApiClient;
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var profile = await _companyApiClient.GetProfileAsync(token, int.Parse(userId));
            var internships = await _internshipApiClient.GetCompanyInternshipsAsync(token, int.Parse(userId));
            var applications = await _applicationApiClient.GetCompanyApplicationsAsync(token, int.Parse(userId));
            var stats = await _applicationApiClient.GetCompanyStatsAsync(token, int.Parse(userId));

            var viewModel = new CompanyDashboardViewModel
            {
                Profile = profile,
                IsVerified = profile?.VerificationStatus == "Verified",
                SubscriptionStatus = profile?.SubscriptionStatus ?? "Inactive",
                RecentInternships = internships?.Take(5).ToList() ?? new List<InternshipResponse>(),
                RecentApplications = applications?.Take(5).ToList() ?? new List<ApplicationResponse>(),
                TotalInternships = stats?.TotalInternships ?? 0,
                ActiveInternships = stats?.ActiveInternships ?? 0,
                TotalApplications = stats?.TotalApplications ?? 0,
                ShortlistedCount = stats?.ShortlistedCount ?? 0
            };

            return View(viewModel);
        }
    }
}