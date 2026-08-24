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
    }
}