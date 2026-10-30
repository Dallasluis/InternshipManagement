# Web stage 3: account links, password reset, verification banner, profile sections

Install web stages 1 and 2 first, then extract this zip over the solution folder.
Overwrites 4 existing files (this zip contains the cumulative versions):
- Services/AiApiClient.cs (token is now optional, for pages used before login)
- Views/Account/Login.cshtml ("Forgot your password?" link)
- Views/Shared/_Layout.cshtml (shows the verification banner)
- Views/Shared/_Sidebar.cshtml (stage 1 links + "Education & Skills")
New: Controllers/AccountRecoveryController.cs, Controllers/ProfileSectionsController.cs, Models/Auth/RecoveryViewModels.cs,
Views/AccountRecovery/{ForgotPassword,ResetPassword,EmailAction}.cshtml, Views/ProfileSections/Index.cshtml,
Views/Shared/_VerifyEmailBanner.cshtml, wwwroot/js/profile-sections.js.
No migration. Build: dotnet build InternshipManagement.Web

## Two small API edits you must make by hand (3 places)

### 1. Route names the Web already uses (required, otherwise profile and internship pages get 404)
The Web calls api/Students/... and api/Internships/..., but the API controllers answer on api/Student and api/Internship.
Add one extra line under the existing [Route("api/[controller]")] in each controller (both addresses then work):

InternshipManagement.Api/Controllers/StudentController.cs
    [Route("api/[controller]")]
    [Route("api/Students")]

InternshipManagement.Api/Controllers/InternshipController.cs
    [Route("api/[controller]")]
    [Route("api/Internships")]

### 2. Make the email links open the Web pages (recommended)
Today the links in emails open plain-text API pages, and the password-reset email shows a raw code.
In InternshipManagement.Application/Services/EmailVerificationOptions.cs add:
    public string? PublicWebBaseUrl { get; set; }

In InternshipManagement.Api/appsettings.json, inside "EmailVerification", add:
    "PublicWebBaseUrl": "http://localhost:5133"

In InternshipManagement.Application/Services/AuthService.cs replace BuildActionLink with:
    private string BuildActionLink(string action, int userId, string token)
    {
        var escaped = Uri.EscapeDataString(token);
        if (!string.IsNullOrWhiteSpace(_emailVerificationOptions.PublicWebBaseUrl))
        {
            var page = action switch
            {
                "verify-email" => "VerifyEmail",
                "confirm-email-change" => "ConfirmEmailChange",
                _ => "CancelEmailChange"
            };
            return $"{_emailVerificationOptions.PublicWebBaseUrl.TrimEnd('/')}/Account/{page}?userId={userId}&token={escaped}";
        }

        var baseUrl = _emailVerificationOptions.PublicApiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/Auth/{action}?userId={userId}&token={escaped}";
    }

And in RequestPasswordResetAsync replace the email body with:
    var resetLink = string.IsNullOrWhiteSpace(_emailVerificationOptions.PublicWebBaseUrl)
        ? null
        : $"{_emailVerificationOptions.PublicWebBaseUrl.TrimEnd('/')}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
    var body = resetLink != null
        ? $"A password reset was requested for your account. Open this link to choose a new password:\n{resetLink}\n\nIf you did not request this, you can ignore this email."
        : $"A password reset was requested for your account. Use this token in the password reset form:\n\n{token}\n\nIf you did not request a password reset, you can ignore this email.";
    await _emailSender.SendEmailAsync(user.Email, "Reset your Internship Management password", body);

## Try it
1. Login page > "Forgot your password?" > enter email > open the email link > choose a new password.
2. Register a new user: a yellow banner asks to verify the email, with "Resend link". Open the email link, press the button.
3. Settings > change email: the confirm and cancel links open the same kind of page.
4. Student sidebar > Education & Skills: add and remove education, experience, skills, certificates, projects.
   (Edit is not included: remove and add again.)
