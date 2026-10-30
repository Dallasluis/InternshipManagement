using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Web.Services;
using InternshipManagement.Web.ViewModels;
using InternshipManagement.Web.Models.Student;
using InternshipManagement.Web.Models.Application;
using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentApiClient _studentApiClient;
        private readonly IApplicationApiClient _applicationApiClient;
        private readonly ILifecycleApiClient _lifecycleApiClient;
        private readonly UploadedFileStorage _uploadedFileStorage;
        private readonly IDocumentApiClient _documentApiClient;

        public StudentController(
            IStudentApiClient studentApiClient,
            IApplicationApiClient applicationApiClient,
            ILifecycleApiClient lifecycleApiClient,
            UploadedFileStorage uploadedFileStorage,
            IDocumentApiClient documentApiClient)
        {
            _studentApiClient = studentApiClient;
            _applicationApiClient = applicationApiClient;
            _lifecycleApiClient = lifecycleApiClient;
            _uploadedFileStorage = uploadedFileStorage;
            _documentApiClient = documentApiClient;
        }

        // GET: /student/dashboard
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

        // GET: /student/profile
        public async Task<IActionResult> Profile()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var profile = await _studentApiClient.GetProfileAsync(token, int.Parse(userId));
            await LoadOwnedDocumentsAsync(token);
            return View(profile ?? new StudentProfileResponse());
        }

        private async Task LoadOwnedDocumentsAsync(string token)
        {
            try { ViewBag.OwnedDocuments = await _documentApiClient.GetOwnedDocumentsAsync(token, HttpContext.RequestAborted); }
            catch (Exception) { ViewBag.OwnedDocuments = null; }
        }

        // POST: /student/profile/remove-document
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDocument(Guid id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var removed = await _documentApiClient.SetRetentionStateAsync(token, id, "Deleted", HttpContext.RequestAborted);
            TempData[removed ? "SuccessMessage" : "ErrorMessage"] = removed ? "Document removed." : "The document could not be removed.";
            return RedirectToAction("Profile");
        }

        // POST: /student/profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(72 * 1024 * 1024)]
        public async Task<IActionResult> Profile(UpdateStudentProfileRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(await _studentApiClient.GetProfileAsync(token, int.Parse(userId)) ?? new StudentProfileResponse());

            var documentUploads = new (IFormFile? File, string Category, Action<Guid> SetId, string ModelField)[]
            {
                (request.ResumeFile, "Resume", id => request.ResumeDocumentId = id, nameof(request.ResumeFile)),
                (request.CoverLetterFile, "CoverLetter", id => request.CoverLetterDocumentId = id, nameof(request.CoverLetterFile)),
                (request.AcademicTranscriptFile, "Transcript", id => request.AcademicTranscriptDocumentId = id, nameof(request.AcademicTranscriptFile)),
                (request.QualificationDocumentFile, "Qualification", id => request.QualificationDocumentId = id, nameof(request.QualificationDocumentFile)),
                (request.IdentificationDocumentFile, "Identification", id => request.IdentificationDocumentId = id, nameof(request.IdentificationDocumentFile)),
                (request.CertificatesFile, "Certificate", id => request.CertificatesDocumentId = id, nameof(request.CertificatesFile)),
                (request.OtherSupportingDocumentsFile, "Supporting", id => request.OtherSupportingDocumentsId = id, nameof(request.OtherSupportingDocumentsFile))
            };

            foreach (var upload in documentUploads)
            {
                if (upload.File is not null && upload.File.Length > 0)
                {
                    try
                    {
                        var document = await _documentApiClient.UploadAsync(
                            token, upload.File, upload.Category, HttpContext.RequestAborted);
                        upload.SetId(document.Id);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
                    {
                        ModelState.AddModelError(upload.ModelField, ex.Message);
                        return View(await _studentApiClient.GetProfileAsync(token, int.Parse(userId)) ?? new StudentProfileResponse());
                    }
                }
            }

            // Extra files added with "Add another file": any number per type, stored as separate documents.
            var extraCategories = new HashSet<string>(StringComparer.Ordinal) { "Transcript", "Qualification", "Certificate", "Supporting" };
            foreach (var extra in Request.Form.Files.Where(file => file.Length > 0 && file.Name.StartsWith("Extra_", StringComparison.Ordinal)))
            {
                var category = extra.Name["Extra_".Length..];
                if (!extraCategories.Contains(category))
                    continue;
                try
                {
                    await _documentApiClient.UploadAsync(token, extra, category, HttpContext.RequestAborted);
                }
                catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
                {
                    ModelState.AddModelError(string.Empty, $"{extra.FileName}: {ex.Message}");
                    await LoadOwnedDocumentsAsync(token);
                    return View(await _studentApiClient.GetProfileAsync(token, int.Parse(userId)) ?? new StudentProfileResponse());
                }
            }

            if (request.ProfilePictureFile is { Length: > 0 })
            {
                try
                {
                    request.ProfilePictureUrl = await _uploadedFileStorage.SaveAsync(
                        request.ProfilePictureFile,
                        "profile-pictures",
                        new[] { ".jpg", ".jpeg", ".png", ".webp" },
                        2 * 1024 * 1024,
                        HttpContext.RequestAborted);
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(nameof(request.ProfilePictureFile), ex.Message);
                    return View(await _studentApiClient.GetProfileAsync(token, int.Parse(userId)) ?? new StudentProfileResponse());
                }
            }

            await _studentApiClient.UpdateProfileAsync(token, int.Parse(userId), request);
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // GET: /student/applications
        public async Task<IActionResult> Applications()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var applications = await _applicationApiClient.GetStudentApplicationsAsync(token, int.Parse(userId));
            return View(applications ?? new List<ApplicationResponse>());
        }

        // GET: /student/applications/{id}
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var application = await _applicationApiClient.GetApplicationByIdAsync(token, id);

            if (application == null)
                return NotFound();

            var viewModel = new ApplicationDetailsViewModel
            {
                Application = application,
                StatusHistory = await _lifecycleApiClient.GetApplicationStatusHistoryAsync(token, id)
            };

            return View(viewModel);
        }

        // POST: /student/applications/{id}/withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var result = await _applicationApiClient.WithdrawAsync(token, id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "Application withdrawn."
                : "This application can no longer be withdrawn. If you accepted an offer, request withdrawal from its placement.";
            return RedirectToAction("Applications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToOffer(int id, bool accepted)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var result = await _applicationApiClient.RespondToOfferAsync(token, id, new RespondToOfferRequest { Accepted = accepted });
            if (result)
                TempData["SuccessMessage"] = accepted
                    ? "Offer accepted. If you have an active placement, request withdrawal from its Placements page. Your new placement starts after the current placement closes."
                    : "Offer declined.";
            else
                TempData["ErrorMessage"] = "Unable to respond to the offer.";

            return RedirectToAction(nameof(ApplicationDetails), new { id });
        }

        public async Task<IActionResult> Placements()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var placements = await _lifecycleApiClient.GetStudentPlacementsAsync(token);
            return View(placements);
        }

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
                Evaluations = await _lifecycleApiClient.GetEvaluationsAsync(token, id)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitProgressReport(int id, SubmitProgressReportRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please complete the required progress report fields.";
                return RedirectToAction(nameof(PlacementDetails), new { id });
            }

            var report = await _lifecycleApiClient.SubmitProgressReportAsync(token, id, request);
            TempData[report == null ? "ErrorMessage" : "SuccessMessage"] = report == null
                ? "Unable to submit the progress report."
                : "Progress report submitted successfully.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestWithdrawal(int id, RequestWithdrawalRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.RequestWithdrawalAsync(token, id, request);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Withdrawal request submitted."
                : "Unable to submit withdrawal request.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToExtension(
            int id,
            int extensionRequestId,
            bool accepted,
            string? notes)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var success = await _lifecycleApiClient.RespondToExtensionAsync(
                token, extensionRequestId, new RespondToExtensionRequest { Accepted = accepted, Notes = notes });
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? accepted ? "Extension accepted." : "Extension declined."
                : "Unable to respond to the extension request.";

            return RedirectToAction(nameof(PlacementDetails), new { id });
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
            if (profile.ResumeDocumentId.HasValue) completion += 15;
            if (profile.CoverLetterDocumentId.HasValue) completion += 10;
            if (profile.AcademicTranscriptDocumentId.HasValue) completion += 10;
            if (profile.QualificationDocumentId.HasValue) completion += 10;
            if (profile.IdentificationDocumentId.HasValue) completion += 10;
            if (profile.Education?.Any() == true) completion += 10;
            if (profile.WorkExperience?.Any() == true) completion += 10;
            if (profile.Skills?.Any() == true) completion += 5;
            return Math.Min(completion, 100);
        }
    }
}
