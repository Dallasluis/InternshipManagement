using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using InternshipManagement.Web.Models.Company;
using InternshipManagement.Web.Models.Internship;
using InternshipManagement.Web.Models.Application;
using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ICompanyApiClient _companyApiClient;
        private readonly IInternshipApiClient _internshipApiClient;
        private readonly IApplicationApiClient _applicationApiClient;
        private readonly ILifecycleApiClient _lifecycleApiClient;

        public CompanyController(
            ICompanyApiClient companyApiClient,
            IInternshipApiClient internshipApiClient,
            IApplicationApiClient applicationApiClient,
            ILifecycleApiClient lifecycleApiClient)
        {
            _companyApiClient = companyApiClient;
            _internshipApiClient = internshipApiClient;
            _applicationApiClient = applicationApiClient;
            _lifecycleApiClient = lifecycleApiClient;
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

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var profile = await _companyApiClient.GetProfileAsync(token, int.Parse(userId));
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateCompanyProfileRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(await _companyApiClient.GetProfileAsync(token, int.Parse(userId)));

            var profile = await _companyApiClient.UpdateProfileAsync(token, int.Parse(userId), request);
            if (profile == null)
            {
                TempData["ErrorMessage"] = "Unable to update company profile.";
                return View(await _companyApiClient.GetProfileAsync(token, int.Parse(userId)));
            }

            TempData["SuccessMessage"] = "Company profile updated successfully.";
            return View(profile);
        }

        [HttpGet]
        public IActionResult CreateInternship()
        {
            return View(new CreateInternshipRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInternship(CreateInternshipRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(request);

            try
            {
                var internship = await _internshipApiClient.CreateInternshipAsync(token, int.Parse(userId), request);
                if (internship == null)
                {
                    TempData["ErrorMessage"] = "The API did not return the created internship.";
                    return View(request);
                }

                TempData["SuccessMessage"] = "Internship posted as a draft.";
                return RedirectToAction(nameof(ManageInternships));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to post internship: {ex.Message}";
                return View(request);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageInternships()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var internships = await _internshipApiClient.GetCompanyInternshipsAsync(token, int.Parse(userId));
            return View(internships);
        }

        [HttpGet]
        public async Task<IActionResult> Applications()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var applications = await _applicationApiClient.GetCompanyApplicationsAsync(token, int.Parse(userId));
            return View("Application", applications);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationReview(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var application = await _applicationApiClient.GetApplicationByIdAsync(token, id);
            if (application == null)
                return NotFound();

            return View(new ApplicationDetailsViewModel
            {
                Application = application,
                StatusHistory = await _lifecycleApiClient.GetApplicationStatusHistoryAsync(token, id)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShortlistApplication(int id, string? notes)
        {
            return await UpdateApplication(id, () => _applicationApiClient.ShortlistAsync(
                GetToken(), id, notes), "shortlisted");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string? notes)
        {
            return await UpdateApplication(id, () => _applicationApiClient.UpdateStatusAsync(
                GetToken(), id, new UpdateApplicationStatusRequest { Status = "Rejected", Notes = notes }), "rejected");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInterview(int id, ScheduleInterviewRequest request)
        {
            if (string.IsNullOrEmpty(GetToken()))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide a valid interview date and type.";
                return RedirectToAction(nameof(ApplicationReview), new { id });
            }

            var success = await _applicationApiClient.ScheduleInterviewAsync(GetToken(), id, request);
            if (success)
                TempData["SuccessMessage"] = "Interview scheduled successfully.";
            else
                TempData["ErrorMessage"] = "Unable to schedule the interview.";

            return RedirectToAction(nameof(ApplicationReview), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInterviewCompleted(int id)
        {
            if (string.IsNullOrEmpty(GetToken()))
                return RedirectToAction("Login", "Account");

            var success = await _applicationApiClient.MarkInterviewCompletedAsync(GetToken(), id);
            if (success)
                TempData["SuccessMessage"] = "Interview marked as completed.";
            else
                TempData["ErrorMessage"] = "Unable to mark the interview as completed.";

            return RedirectToAction(nameof(ApplicationReview), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeOffer(int id, MakeOfferRequest request)
        {
            if (string.IsNullOrEmpty(GetToken()))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide a valid offer start date and expiry date.";
                return RedirectToAction(nameof(ApplicationReview), new { id });
            }

            var success = await _applicationApiClient.MakeOfferAsync(GetToken(), id, request);
            if (success)
                TempData["SuccessMessage"] = "Offer sent successfully.";
            else
                TempData["ErrorMessage"] = "Unable to send the offer.";

            return RedirectToAction(nameof(ApplicationReview), new { id });
        }

        private string GetToken() => HttpContext.Session.GetString("JwtToken") ?? string.Empty;

        private async Task<IActionResult> UpdateApplication(int id, Func<Task<bool>> update, string status)
        {
            if (string.IsNullOrEmpty(GetToken()))
                return RedirectToAction("Login", "Account");

            try
            {
                if (!await update())
                    TempData["ErrorMessage"] = $"The application could not be {status}.";
                else
                    TempData["SuccessMessage"] = $"Application {status} successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to update application: {ex.Message}";
            }

            return RedirectToAction(nameof(Applications));
        }

        [HttpGet]
        public async Task<IActionResult> EditInternship(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var internship = await _internshipApiClient.GetByIdAsync(id);
            if (internship == null)
                return NotFound();

            return View(new UpdateInternshipRequest
            {
                Id = internship.Id,
                Title = internship.Title,
                Description = internship.Description,
                Industry = internship.Industry,
                Location = internship.Location,
                IsRemote = internship.IsRemote,
                InternshipType = internship.InternshipType,
                Duration = internship.Duration,
                StartDate = internship.StartDate,
                EndDate = internship.EndDate,
                ApplicationDeadline = internship.ApplicationDeadline,
                NumberOfPositions = internship.NumberOfPositions,
                Compensation = internship.Compensation,
                StipendAmount = internship.StipendAmount,
                Currency = internship.Currency,
                Skills = internship.Skills,
                EligibleProgrammes = internship.EligibleProgrammes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInternship(UpdateInternshipRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(request);

            try
            {
                await _internshipApiClient.UpdateInternshipAsync(token, request.Id, request);
                TempData["SuccessMessage"] = "Internship updated successfully.";
                return RedirectToAction(nameof(ManageInternships));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to update internship: {ex.Message}";
                return View(request);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishInternship(int id)
        {
            return await ChangeInternshipStatus(id, "published", (token, internshipId) =>
                _internshipApiClient.PublishInternshipAsync(token, internshipId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseInternship(int id)
        {
            return await ChangeInternshipStatus(id, "closed", (token, internshipId) =>
                _internshipApiClient.CloseInternshipAsync(token, internshipId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInternship(int id)
        {
            return await ChangeInternshipStatus(id, "deleted", (token, internshipId) =>
                _internshipApiClient.DeleteInternshipAsync(token, internshipId));
        }

        private async Task<IActionResult> ChangeInternshipStatus(
            int id,
            string actionDescription,
            Func<string, int, Task<bool>> action)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                if (!await action(token, id))
                {
                    TempData["ErrorMessage"] = $"The internship could not be {actionDescription}.";
                    return RedirectToAction(nameof(ManageInternships));
                }

                TempData["SuccessMessage"] = $"Internship {actionDescription} successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to {actionDescription} internship: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageInternships));
        }

        [HttpGet]
        public async Task<IActionResult> Placements()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var placements = await _lifecycleApiClient.GetCompanyPlacementsAsync(token);
            return View(placements);
        }

        [HttpGet]
        public async Task<IActionResult> PlacementDetails(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var placement = await _lifecycleApiClient.GetPlacementAsync(token, id);
            if (placement == null)
                return NotFound();

            return View(new PlacementDetailsViewModel
            {
                Placement = placement,
                ProgressReports = await _lifecycleApiClient.GetProgressReportsAsync(token, id),
                Evaluations = await _lifecycleApiClient.GetEvaluationsAsync(token, id),
                IsCompanyView = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewProgressReport(int placementId, int reportId, ReviewProgressReportRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.ReviewProgressReportAsync(token, reportId, request);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Progress report reviewed."
                : "Unable to review progress report.";

            return RedirectToAction(nameof(PlacementDetails), new { id = placementId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeExtension(int id, ProposeExtensionRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.ProposeExtensionAsync(token, id, request);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Extension proposed."
                : "Unable to propose extension.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePlacement(int id, CompletePlacementRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.CompletePlacementAsync(token, id, request);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Placement marked as completed."
                : "Unable to complete placement.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminatePlacement(int id, TerminatePlacementRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.TerminatePlacementAsync(token, id, request);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Placement terminated."
                : "Unable to terminate placement.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvaluation(int id, SubmitEvaluationRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var evaluation = await _lifecycleApiClient.SubmitEvaluationAsync(token, id, request);
            TempData[evaluation == null ? "ErrorMessage" : "SuccessMessage"] = evaluation == null
                ? "Unable to submit evaluation."
                : "Evaluation submitted.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }
    }
}
