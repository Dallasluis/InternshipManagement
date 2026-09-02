using InternshipManagement.Web.Models.Application;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly IApplicationApiClient _applicationApiClient;

        public ApplicationsController(IApplicationApiClient applicationApiClient)
        {
            _applicationApiClient = applicationApiClient;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyRequest request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "A valid internship is required.";
                return RedirectToAction("Details", "Internships", new { id = request.InternshipId });
            }

            try
            {
                var application = await _applicationApiClient.ApplyAsync(token, int.Parse(userId), request);
                if (application == null)
                    TempData["ErrorMessage"] = "The application was not created.";
                else
                    TempData["SuccessMessage"] = "Application submitted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to apply: {ex.Message}";
            }

            return RedirectToAction("Details", "Internships", new { id = request.InternshipId });
        }
    }
}
