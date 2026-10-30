using System.Text;
using System.Text.Json;
using InternshipManagement.Web.Models.Auth;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// Pages for the links in the emails the API sends (verify email, confirm or cancel an email change,
    /// reset password) plus "forgot password" and "resend the verification email".
    /// The email links open a page with a button instead of acting at once, so a mail scanner that
    /// opens every link cannot use up a one-time link by accident.
    /// </summary>
    [Route("Account")]
    public class AccountRecoveryController : Controller
    {
        private readonly IAiApiClient _api;

        public AccountRecoveryController(IAiApiClient api)
        {
            _api = api;
        }

        // ------------------------------------------------------------------ forgot / reset password

        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost("ForgotPassword"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.SendAsync(string.Empty, HttpMethod.Post, "api/Auth/forgot-password", Json(new { email = model.Email }), ct);
            if (result.StatusCode >= 500 || result.StatusCode == 429)
            {
                ModelState.AddModelError(string.Empty, "We could not send the email right now. Please try again in a few minutes.");
                return View(model);
            }

            // Same answer whether or not the address has an account, so this page cannot be used to find out who is registered.
            TempData["StatusMessage"] = "If an account exists for that email address, we have sent a link to reset the password.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword(string? email, string? token) =>
            View(new ResetPasswordViewModel { Email = email ?? string.Empty, Token = token ?? string.Empty });

        [HttpPost("ResetPassword"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
        {
            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "The passwords do not match.");
            if (!ModelState.IsValid)
                return View(model);

            var result = await _api.SendAsync(string.Empty, HttpMethod.Post, "api/Auth/reset-password",
                Json(new { email = model.Email, token = model.Token, newPassword = model.NewPassword }), ct);

            if (result.StatusCode >= 500)
            {
                ModelState.AddModelError(string.Empty, "We could not reset the password right now. Please try again in a few minutes.");
                return View(model);
            }

            if (!ReadSuccess(result.Body))
            {
                ModelState.AddModelError(string.Empty,
                    "The password could not be reset. The link may have expired or been used already, " +
                    "or the new password is not accepted (it needs at least 10 characters and must not be a password found in known data breaches). " +
                    "You can request a new link from the login page.");
                return View(model);
            }

            TempData["StatusMessage"] = "Your password has been changed. You can now sign in.";
            return RedirectToAction("Login", "Account");
        }

        // ------------------------------------------------------------------ links in emails

        [HttpGet("VerifyEmail")]
        public IActionResult VerifyEmail(int userId, string? token) => EmailActionPage("verify", userId, token);

        [HttpPost("VerifyEmail"), ValidateAntiForgeryToken]
        public Task<IActionResult> VerifyEmailConfirmed(int userId, string? token, CancellationToken ct) =>
            RunEmailAction("verify", userId, token, ct);

        [HttpGet("ConfirmEmailChange")]
        public IActionResult ConfirmEmailChange(int userId, string? token) => EmailActionPage("confirm-change", userId, token);

        [HttpPost("ConfirmEmailChange"), ValidateAntiForgeryToken]
        public Task<IActionResult> ConfirmEmailChangeConfirmed(int userId, string? token, CancellationToken ct) =>
            RunEmailAction("confirm-change", userId, token, ct);

        [HttpGet("CancelEmailChange")]
        public IActionResult CancelEmailChange(int userId, string? token) => EmailActionPage("cancel-change", userId, token);

        [HttpPost("CancelEmailChange"), ValidateAntiForgeryToken]
        public Task<IActionResult> CancelEmailChangeConfirmed(int userId, string? token, CancellationToken ct) =>
            RunEmailAction("cancel-change", userId, token, ct);

        // ------------------------------------------------------------------ resend the verification email

        [HttpPost("ResendVerification"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(string? returnUrl, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Post, "api/Auth/resend-verification", null, ct);
            TempData["VerificationMessage"] = result.StatusCode switch
            {
                >= 200 and < 300 => "A new verification link has been sent. Check your inbox (and spam folder).",
                429 => "You have asked for too many links. Please wait a while before trying again.",
                _ => ReadMessage(result.Body) ?? "We could not send the link right now. Please try again later."
            };

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }

        // ------------------------------------------------------------------ "I have verified": refresh the signed-in details

        [HttpPost("RefreshSession"), ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshSession(string? returnUrl, [FromServices] TokenRefreshCoordinator coordinator, CancellationToken ct)
        {
            var outcome = await coordinator.EnsureFreshAsync(HttpContext.Session, force: true, ct);
            if (outcome == RefreshOutcome.NoSession || outcome == RefreshOutcome.Expired)
                return RedirectToAction("Login", "Account");

            TempData["VerificationMessage"] = outcome == RefreshOutcome.Refreshed
                ? (HttpContext.Session.GetString("EmailConfirmed") == "True"
                    ? "Thank you, your email address is verified."
                    : "Your email address is not verified yet. Open the link we sent you, then try again.")
                : "We could not check right now. Please try again in a moment.";

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }

        // ------------------------------------------------------------------ helpers

        private IActionResult EmailActionPage(string kind, int userId, string? token)
        {
            var model = Describe(kind, userId, token ?? string.Empty);
            if (userId <= 0 || string.IsNullOrWhiteSpace(token))
            {
                model.Done = true;
                model.Success = false;
                model.Message = "This link is incomplete. Open the link from your email again.";
            }
            return View("EmailAction", model);
        }

        private async Task<IActionResult> RunEmailAction(string kind, int userId, string? token, CancellationToken ct)
        {
            var model = Describe(kind, userId, token ?? string.Empty);
            model.Done = true;

            if (userId <= 0 || string.IsNullOrWhiteSpace(token))
            {
                model.Message = "This link is incomplete. Open the link from your email again.";
                return View("EmailAction", model);
            }

            var path = kind switch
            {
                "verify" => "api/Auth/verify-email",
                "confirm-change" => "api/Auth/confirm-email-change",
                _ => "api/Auth/cancel-email-change"
            };

            var result = await _api.SendAsync(string.Empty, HttpMethod.Get,
                $"{path}?userId={userId}&token={Uri.EscapeDataString(token)}", null, ct);

            if (result.StatusCode >= 200 && result.StatusCode < 300)
            {
                model.Success = true;
                model.Message = kind switch
                {
                    "verify" => "Your email address is verified. You can now use all features.",
                    "confirm-change" => "Your email address has been changed. Sign in with the new address.",
                    _ => "The email change was cancelled. Your account still uses your current address. If this was not you, change your password."
                };
                if (kind != "cancel-change")
                    HttpContext.Session.Clear();   // the signed-in details in this browser are out of date now
            }
            else
            {
                model.Success = false;
                model.Message = result.StatusCode >= 500
                    ? "We could not complete this right now. Please try the link again in a few minutes."
                    : "This link is invalid, has expired, or was already used. Sign in and request a new one if you still need it.";
            }

            return View("EmailAction", model);
        }

        private static EmailActionViewModel Describe(string kind, int userId, string token) => kind switch
        {
            "verify" => new EmailActionViewModel
            {
                Kind = kind, UserId = userId, Token = token,
                Heading = "Verify your email address",
                Prompt = "Press the button to confirm that this email address is yours.",
                ButtonText = "Verify my email"
            },
            "confirm-change" => new EmailActionViewModel
            {
                Kind = kind, UserId = userId, Token = token,
                Heading = "Confirm your new email address",
                Prompt = "Press the button to start using this address for your account.",
                ButtonText = "Confirm new email"
            },
            _ => new EmailActionViewModel
            {
                Kind = kind, UserId = userId, Token = token,
                Heading = "Cancel the email change",
                Prompt = "Someone asked to change the email address on your account. If that was not you, cancel it here.",
                ButtonText = "Cancel the change"
            }
        };

        private static StringContent Json(object value) =>
            new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

        private static bool ReadSuccess(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var property in doc.RootElement.EnumerateObject())
                    if (property.Name.Equals("success", StringComparison.OrdinalIgnoreCase))
                        return property.Value.ValueKind == JsonValueKind.True;
            }
            catch (JsonException) { }
            catch (InvalidOperationException) { }
            return false;
        }

        private static string? ReadMessage(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var property in doc.RootElement.EnumerateObject())
                    if ((property.Name.Equals("message", StringComparison.OrdinalIgnoreCase)
                         || property.Name.Equals("error", StringComparison.OrdinalIgnoreCase))
                        && property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();
            }
            catch (JsonException) { }
            catch (InvalidOperationException) { }
            return null;
        }
    }
}
