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

        private string? Token => HttpContext.Session.GetString("JwtToken");

        public async Task<IActionResult> Dashboard()
        {
            var token = Token;
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

        public async Task<IActionResult> Companies()
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var companies = await _adminApiClient.GetAllCompaniesAsync(token);
            return View(companies);
        }

        public async Task<IActionResult> CompanyDetails(int id)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var companies = await _adminApiClient.GetAllCompaniesAsync(token);
            var company = companies.FirstOrDefault(c => c.Id == id);

            if (company == null)
                return NotFound();

            ViewBag.VerificationHistory = await _adminApiClient.GetVerificationHistoryAsync(token, id);
            ViewBag.VerificationEvidence = await _adminApiClient.GetVerificationEvidenceAsync(token, id);
            return View(company);
        }

        public async Task<IActionResult> Internships()
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var internships = await _adminApiClient.GetAllInternshipsAsync(token);
            return View(internships);
        }

        public async Task<IActionResult> Reports()
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var reports = await _adminApiClient.GetAllReportsAsync(token);
            return View(reports);
        }

        public async Task<IActionResult> Users()
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var users = await _adminApiClient.GetAllUsersAsync(token);
            return View(users);
        }

        // ---- AJAX action endpoints used by the views' JS (calls the API server-side, using the session JWT) ----

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCompany(
            int id,
            string status,
            string reasonCode,
            string notes,
            bool evidenceReviewed,
            bool registrationReviewed,
            bool addressReviewed,
            bool isAppeal = false)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            if (string.IsNullOrWhiteSpace(reasonCode) || string.IsNullOrWhiteSpace(notes))
                return Json(new { success = false, message = "A reason code and decision notes are required." });

            var criteria = new Dictionary<string, bool>
            {
                ["EvidenceReviewed"] = evidenceReviewed,
                ["RegistrationReviewed"] = registrationReviewed,
                ["AddressReviewed"] = addressReviewed
            };
            var result = await _adminApiClient.ReviewVerificationAsync(
                token, id, status, reasonCode, notes, criteria, isAppeal);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> SuspendCompany(int companyId, bool suspend)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            var companies = await _adminApiClient.GetAllCompaniesAsync(token);
            var company = companies.FirstOrDefault(c => c.Id == companyId);
            if (company == null)
                return Json(new { success = false, message = "Company not found" });

            var result = await _adminApiClient.SuspendUserAsync(token, company.UserId, suspend);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> ModerateInternship(int id, string status)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            var result = await _adminApiClient.ModerateInternshipAsync(token, id, status, null);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(
            int id,
            bool resolved,
            string? status,
            bool fraudUpheld = false,
            string? response = null)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            var result = await _adminApiClient.ResolveReportAsync(
                token, id, response ?? string.Empty, resolved, status, fraudUpheld);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> SuspendUser(int id, bool suspend)
        {
            var token = Token;
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            var result = await _adminApiClient.SuspendUserAsync(token, id, suspend);
            return Json(new { success = result });
        }
    }
}
