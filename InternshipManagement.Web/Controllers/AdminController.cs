using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using InternshipManagement.Web.Models.Admin;

namespace InternshipManagement.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminApiClient _adminApiClient;

        public AdminController(IAdminApiClient adminApiClient)
        {
            _adminApiClient = adminApiClient;
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var stats = await _adminApiClient.GetStatsAsync(token);
            var companies = await _adminApiClient.GetRecentCompaniesAsync(token, 5);
            var reports = await _adminApiClient.GetRecentReportsAsync(token, 5);

            var viewModel = new AdminDashboardViewModel
            {
                Stats = stats,
                RecentCompanies = companies ?? new List<CompanySummaryResponse>(),
                RecentReports = reports ?? new List<ReportSummaryResponse>()
            };

            return View(viewModel);
        }
    }
}