using InternshipManagement.Web.Models.Auth;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthApiClient _authApiClient;

        public AccountController(IAuthApiClient authApiClient)
        {
            _authApiClient = authApiClient;
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
                foreach (var error in result.Errors ?? new List<string>())
                    ModelState.AddModelError(string.Empty, error);

                return View(model);
            }

            // Store JWT and user info in Session
            HttpContext.Session.SetString("JwtToken", result.Token ?? string.Empty);
            HttpContext.Session.SetString("UserId", result.UserId ?? string.Empty);
            HttpContext.Session.SetString("UserType", result.UserType ?? string.Empty);
            HttpContext.Session.SetString("UserEmail", result.Email ?? string.Empty);
            HttpContext.Session.SetString("UserFullName", $"{result.FirstName} {result.LastName}".Trim());

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
        public IActionResult Logout()
        {
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
                Preferences = await _authApiClient.GetPreferencesAsync(token) ?? new AccountPreferencesViewModel()
            });
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