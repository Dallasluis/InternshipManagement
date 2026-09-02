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
        private readonly IWebHostEnvironment _environment;

        public StudentController(
            IStudentApiClient studentApiClient,
            IApplicationApiClient applicationApiClient,
            IWebHostEnvironment environment)
        {
            _studentApiClient = studentApiClient;
            _applicationApiClient = applicationApiClient;
            _environment = environment;
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
            return View(profile ?? new StudentProfileResponse());
        }

        // POST: /student/profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateStudentProfileRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var fileUploadMap = new[]
            {
                new { File = request.ResumeFile, Property = nameof(UpdateStudentProfileRequest.ResumeUrl), Folder = "resumes", AllowedExtensions = new[] { ".pdf", ".doc", ".docx" } },
                new { File = request.CoverLetterFile, Property = nameof(UpdateStudentProfileRequest.CoverLetterUrl), Folder = "cover-letters", AllowedExtensions = new[] { ".pdf", ".doc", ".docx" } },
                new { File = request.AcademicTranscriptFile, Property = nameof(UpdateStudentProfileRequest.AcademicTranscriptUrl), Folder = "transcripts", AllowedExtensions = new[] { ".pdf", ".doc", ".docx" } },
                new { File = request.QualificationDocumentFile, Property = nameof(UpdateStudentProfileRequest.QualificationDocumentUrl), Folder = "qualifications", AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" } },
                new { File = request.IdentificationDocumentFile, Property = nameof(UpdateStudentProfileRequest.IdentificationDocumentUrl), Folder = "ids", AllowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" } },
                new { File = request.CertificatesFile, Property = nameof(UpdateStudentProfileRequest.CertificatesUrl), Folder = "certificates", AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" } },
                new { File = request.OtherSupportingDocumentsFile, Property = nameof(UpdateStudentProfileRequest.OtherSupportingDocumentsUrl), Folder = "supporting-documents", AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" } }
            };

            const long maxFileSize = 5 * 1024 * 1024;

            foreach (var upload in fileUploadMap)
            {
                if (upload.File is not null && upload.File.Length > 0)
                {
                    var extension = Path.GetExtension(upload.File.FileName).ToLowerInvariant();

                    if (upload.File.Length > maxFileSize || !upload.AllowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(upload.Property, $"Please upload a valid file for {upload.Property.Replace("Url", "")} no larger than 5 MB.");
                        return View(request);
                    }

                    var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", upload.Folder);
                    Directory.CreateDirectory(uploadDirectory);
                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    await using var stream = new FileStream(filePath, FileMode.CreateNew);
                    await upload.File.CopyToAsync(stream);

                    var property = typeof(UpdateStudentProfileRequest).GetProperty(upload.Property);
                    if (property != null)
                    {
                        property.SetValue(request, $"/uploads/{upload.Folder}/{fileName}");
                    }
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

            return View(application);
        }

        // POST: /student/applications/{id}/withdraw
        [HttpPost]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var token = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Not authenticated" });

            var result = await _applicationApiClient.WithdrawAsync(token, id);
            return Json(new { success = result });
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
                TempData["SuccessMessage"] = accepted ? "Offer accepted successfully." : "Offer declined.";
            else
                TempData["ErrorMessage"] = "Unable to respond to the offer.";

            return RedirectToAction(nameof(ApplicationDetails), new { id });
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
            if (!string.IsNullOrEmpty(profile.CoverLetterUrl)) completion += 10;
            if (!string.IsNullOrEmpty(profile.AcademicTranscriptUrl)) completion += 10;
            if (!string.IsNullOrEmpty(profile.QualificationDocumentUrl)) completion += 10;
            if (!string.IsNullOrEmpty(profile.IdentificationDocumentUrl)) completion += 10;
            if (profile.Education?.Any() == true) completion += 10;
            if (profile.WorkExperience?.Any() == true) completion += 10;
            if (profile.Skills?.Any() == true) completion += 5;
            return Math.Min(completion, 100);
        }
    }
}