using System.Net.Http.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>A company moves or cancels an interview that is already scheduled.</summary>
    [Route("Company")]
    public class CompanyInterviewsController : Controller
    {
        private readonly IAiApiClient _api;

        public CompanyInterviewsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpPost("RescheduleInterview/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(
            int id, DateTime interviewDateTime, string? interviewType,
            string? interviewLocationOrLink, string? interviewNotes, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            if (interviewDateTime == default || string.IsNullOrWhiteSpace(interviewType))
            {
                TempData["ErrorMessage"] = "Please choose a new date and time, and an interview type.";
                return Back(id);
            }

            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/Applications/{id}/reschedule-interview",
                JsonContent.Create(new
                {
                    InterviewDateTime = interviewDateTime,
                    InterviewType = interviewType.Trim(),
                    InterviewLocationOrLink = interviewLocationOrLink,
                    InterviewNotes = interviewNotes
                }), ct);

            Report(result, "Interview rescheduled. The candidate has been told.", "The interview could not be rescheduled.");
            return Back(id);
        }

        [HttpPost("CancelInterview/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/Applications/{id}/cancel-interview",
                JsonContent.Create(new { Reason = reason }), ct);

            Report(result, "Interview cancelled. The candidate is back on the shortlist and has been told.", "The interview could not be cancelled.");
            return Back(id);
        }

        private void Report(ApiProxyResult result, string success, string failure)
        {
            if (result.StatusCode is >= 200 and < 300)
                TempData["SuccessMessage"] = success;
            else
                TempData["ErrorMessage"] = ApiErrorReader.Read(result.Body, failure);
        }

        private IActionResult Back(int id) => RedirectToAction("ApplicationReview", "Company", new { id });
    }
}
