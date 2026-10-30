using InternshipManagement.Web.Models.Auth;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly UploadedFileStorage _uploadedFileStorage;
        private readonly ITwoFactorApiClient _twoFactorApiClient;

        public AccountController(
            IAuthApiClient authApiClient,
            UploadedFileStorage uploadedFileStorage,
            ITwoFactorApiClient twoFactorApiClient)
        {
            _authApiClient = authApiClient;
            _uploadedFileStorage = uploadedFileStorage;
            _twoFactorApiClient = twoFactorApiClient;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authApiClient.RegisterAsync(model);

            if (!result.Success)
            {
                foreach (var error in result.Errors ?? new List<string>())
                    ModelState.AddModelError(string.Empty, error);

                return View(model);
            }

            TempData["StatusMessage"] = result.Message ?? "Registration successful. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authApiClient.LoginAsync(model);

            if (!result.Success)
            {
                if (result.RequiresTwoFactor)
                {
                    model.RequiresTwoFactor = true;
                    ModelState.AddModelError(string.Empty, result.Message ?? "Enter the 6-digit code we emailed you.");
                    return View(model);
                }

                foreach (var error in result.Errors ?? new List<string>())
                    ModelState.AddModelError(string.Empty, error);

                return View(model);
            }

            if (result.RequiresTwoFactorSetup)
            {
                HttpContext.Session.SetString("JwtToken", result.Token ?? string.Empty);
                HttpContext.Session.SetString("UserId", result.UserId ?? string.Empty);
                HttpContext.Session.SetString("UserType", result.UserType ?? string.Empty);
                return RedirectToAction(nameof(TwoFactorSetup));
            }

            // Store JWT and user info in Session
            HttpContext.Session.SetString("JwtToken", result.Token ?? string.Empty);
            HttpContext.Session.SetString("UserId", result.UserId ?? string.Empty);
            HttpContext.Session.SetString("UserType", result.UserType ?? string.Empty);
            HttpContext.Session.SetString("UserEmail", result.Email ?? string.Empty);
            HttpContext.Session.SetString("UserFullName", $"{result.FirstName} {result.LastName}".Trim());
            HttpContext.Session.SetString("ProfilePictureUrl", result.ProfilePictureUrl ?? string.Empty);
            HttpContext.Session.SetString("EmailConfirmed", result.EmailConfirmed.ToString());
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken ?? string.Empty);

            // Redirect based on user type
            return result.UserType switch
            {
                "Student" => RedirectToAction("Dashboard", "Student"),
                "Company" => RedirectToAction("Dashboard", "Company"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout([FromServices] IAiApiClient api)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                // Ends the session on the API too, so the refresh token cannot be used again from somewhere else.
                // Best effort: the user is signed out here whether or not the API answers.
                int.TryParse(HttpContext.Session.GetString("UserId"), out var userId);
                await api.SendAsync(token, HttpMethod.Post, "api/Auth/logout",
                    new StringContent($"{{\"userId\":{userId}}}", System.Text.Encoding.UTF8, "application/json"));
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));

            ViewData["Title"] = "Settings";
            return View(new AccountSettingsViewModel
            {
                UserFullName = HttpContext.Session.GetString("UserFullName"),
                UserEmail = HttpContext.Session.GetString("UserEmail"),
                UserType = HttpContext.Session.GetString("UserType"),
                ProfilePictureUrl = HttpContext.Session.GetString("ProfilePictureUrl"),
                Preferences = await _authApiClient.GetPreferencesAsync(token) ?? new AccountPreferencesViewModel()
            });
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorSetup()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            var setup = await _twoFactorApiClient.GetSetupAsync(token);
            if (setup == null)
            {
                TempData["ErrorMessage"] = "Unable to load authenticator setup.";
                return RedirectToAction(nameof(Settings));
            }

            return View(setup);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(TwoFactorSetupViewModel model)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            var result = await _twoFactorApiClient.EnableAsync(token, model.Code);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors ?? new List<string> { "Unable to enable two-factor authentication." });
                return RedirectToAction(nameof(TwoFactorSetup));
            }

            HttpContext.Session.Clear();
            TempData["StatusMessage"] = result.Message ?? "Two-factor authentication enabled. Sign in again.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile? profilePicture)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            if (profilePicture == null)
            {
                TempData["ErrorMessage"] = "Choose a profile picture to upload.";
                return RedirectToAction(nameof(Settings));
            }

            try
            {
                var pictureUrl = await _uploadedFileStorage.SaveAsync(
                    profilePicture,
                    "profile-pictures",
                    new[] { ".jpg", ".jpeg", ".png", ".webp" },
                    2 * 1024 * 1024,
                    HttpContext.RequestAborted);

                var result = await _authApiClient.UpdateProfilePictureAsync(token, pictureUrl);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = string.Join(" ", result.Errors ?? new List<string> { "Unable to update profile picture." });
                    return RedirectToAction(nameof(Settings));
                }

                HttpContext.Session.SetString("ProfilePictureUrl", pictureUrl);
                TempData["SuccessMessage"] = "Profile picture updated.";
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Settings));

            var result = await _authApiClient.ChangePasswordAsync(HttpContext.Session.GetString("JwtToken") ?? string.Empty, model);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Password changed successfully."
                : string.Join(" ", result.Errors ?? new List<string> { "Unable to change password." });
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            var result = await _authApiClient.ChangeEmailAsync(HttpContext.Session.GetString("JwtToken") ?? string.Empty, model);
            if (result.Success)
            {
                HttpContext.Session.Clear();
                TempData["StatusMessage"] = "Email changed successfully. Please log in again.";
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = string.Join(" ", result.Errors ?? new List<string> { "Unable to change email." });
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePreferences(AccountPreferencesViewModel model)
        {
            var result = await _authApiClient.UpdatePreferencesAsync(HttpContext.Session.GetString("JwtToken") ?? string.Empty, model);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Preferences saved successfully."
                : string.Join(" ", result.Errors ?? new List<string> { "Unable to save preferences." });
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate()
        {
            var result = await _authApiClient.DeactivateAsync(HttpContext.Session.GetString("JwtToken") ?? string.Empty);
            if (result.Success)
            {
                HttpContext.Session.Clear();
                TempData["StatusMessage"] = "Your account has been deactivated.";
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = "Unable to deactivate your account.";
            return RedirectToAction(nameof(Settings));
        }
    }
}