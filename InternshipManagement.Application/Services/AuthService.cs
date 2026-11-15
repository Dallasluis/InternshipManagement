using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using InternshipManagement.Application.DTOs.Auth;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace InternshipManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private const string LoginChallengeTokenName = "EmailOtp:LoginChallenge";
        private readonly IApplicationDbContext _context;
        private readonly IIdentityService _identityService;
        private readonly IEmailSender _emailSender;
        private readonly EmailVerificationOptions _emailVerificationOptions;
        private readonly LoginSecurityOptions _loginSecurityOptions;
        private readonly IPasswordBreachChecker _passwordBreachChecker;

        public AuthService(
            IApplicationDbContext context,
            IIdentityService identityService,
            IEmailSender emailSender,
            IOptions<EmailVerificationOptions> emailVerificationOptions,
            IOptions<LoginSecurityOptions> loginSecurityOptions,
            IPasswordBreachChecker passwordBreachChecker)
        {
            _context = context;
            _identityService = identityService;
            _emailSender = emailSender;
            _emailVerificationOptions = emailVerificationOptions.Value;
            _loginSecurityOptions = loginSecurityOptions.Value;
            _passwordBreachChecker = passwordBreachChecker;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _passwordBreachChecker.IsCompromisedAsync(request.Password))
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Choose a password that has not appeared in known data breaches." }
                };
            }

            var userType = string.IsNullOrWhiteSpace(request.UserType)
                ? "Student"
                : request.UserType.Trim();

            if (string.Equals(userType, "Student", StringComparison.OrdinalIgnoreCase))
                userType = "Student";
            else if (string.Equals(userType, "Company", StringComparison.OrdinalIgnoreCase))
                userType = "Company";
            else
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Public registration is limited to student and company accounts." }
                };
            }

            // Check if user exists
            var existingUser = await _identityService.FindUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "User with this email already exists" }
                };
            }

            // Create user using IdentityService
            var result = await _identityService.CreateUserAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                userType
            );

            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = result.Errors
                };
            }

            // Assign role
            var role = userType;
            if (!await _identityService.RoleExistsAsync(role))
            {
                await _identityService.CreateRoleAsync(role);
            }
            await _identityService.AddToRoleAsync(result.UserId, role);

            // Create profile based on user type (only for Company and Student)
            var userId = int.Parse(result.UserId);
            if (userType == "Company")
            {
                var companyProfile = new CompanyProfile
                {
                    UserId = userId,
                    CompanyName = request.CompanyName ?? $"{request.FirstName} {request.LastName}'s Company",
                    Industry = request.Industry ?? string.Empty,
                    VerificationStatus = CompanyVerificationStatus.Pending,
                    IsSubscribed = false
                };
                _context.CompanyProfiles.Add(companyProfile);
            }
            else
            {
                var studentProfile = new StudentProfile
                {
                    UserId = userId,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };
                _context.StudentProfiles.Add(studentProfile);
            }

            await _context.SaveChangesAsync();

            var emailToken = await IssueEmailActionTokenAsync(userId, "RegistrationVerification");
            await _emailSender.SendEmailAsync(
                request.Email,
                "Verify your Internship Management account",
                $"Hello {request.FirstName},\n\nVerify your email within {_emailVerificationOptions.LinkExpiryHours} hours:\n{BuildActionLink("verify-email", userId, emailToken)}\n\nIf you did not create this account, you can ignore this email.");

            return new AuthResponse
            {
                Success = true,
                UserId = result.UserId,
                UserType = userType,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Message = "Registration successful. Please verify your email."
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _identityService.FindUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password" }
                };
            }

            var signInResult = await _identityService.SignInAsync(request.Email, request.Password, false);
            if (!signInResult.Succeeded)
            {
                if (signInResult.IsNewlyLockedOut)
                {
                    await RecordSecurityEventAsync(user.Id, "AccountLockedOut", request.ClientIpAddress);
                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Your Internship Management account was locked",
                        "We detected repeated unsuccessful sign-in attempts. Your account is temporarily locked. If this was not you, contact support.");
                    if (user.UserType.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        await NotifyOtherAdminsAsync(
                            user.Id,
                            $"Administrator account {user.Email} was locked after repeated failed sign-in attempts. IP: {request.ClientIpAddress ?? "unknown"}.");
                    }
                }
                await RecordSecurityEventAsync(user.Id, signInResult.IsLockedOut ? "LoginAttemptDuringLockout" : "LoginFailed", request.ClientIpAddress);
                return new AuthResponse { Success = false, Errors = new List<string> { "Invalid email or password." } };
            }

            if (!user.IsActive)
            {
                await RecordSecurityEventAsync(user.Id, "LoginAttemptForInactiveAccount", request.ClientIpAddress);
                return new AuthResponse { Success = false, Errors = new List<string> { "Invalid email or password." } };
            }

            if (RequiresEmailOtp(user.UserType))
            {
                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    var sent = await SendOtpAsync(user, "Login");
                    var challengeId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    await _identityService.SetUserTokenAsync(user.Id.ToString(), LoginChallengeTokenName, challengeId);
                    return new AuthResponse
                    {
                        Success = false,
                        RequiresTwoFactor = true,
                        TwoFactorChallengeToken = await _identityService.GenerateJwtTokenAsync(
                            user.Id.ToString(),
                            rememberMe: request.RememberMe && user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase),
                            twoFactorLoginOnly: true,
                            loginChallengeId: challengeId),
                        Message = sent
                            ? $"We emailed a 6-digit code to {MaskEmail(user.Email)}. It is valid for {_loginSecurityOptions.OtpLifetimeMinutes} minutes."
                            : "A code was sent a moment ago. Check your email, or wait a minute before asking for another."
                    };
                }

                if (!await VerifyOtpAsync(user.Id, "Login", request.TwoFactorCode))
                {
                    await RecordSecurityEventAsync(user.Id, "OtpLoginFailed", request.ClientIpAddress);
                    return new AuthResponse
                    {
                        Success = false,
                        RequiresTwoFactor = true,
                        Errors = new List<string> { "The code is incorrect or has expired. Request a new code and try again." }
                    };
                }
            }

            var rememberMe = request.RememberMe && user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase);
            return await CreateLoginResponseAsync(user, rememberMe, request.ClientIpAddress);
        }

        public async Task<AuthResponse> CompleteTwoFactorLoginAsync(int userId, string challengeId, string code, bool rememberMe, string? ipAddress)
        {
            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            var activeChallenge = await _identityService.GetUserTokenAsync(userId.ToString(), LoginChallengeTokenName);
            if (user == null || !user.IsActive || !IsSameChallenge(activeChallenge, challengeId))
                return new AuthResponse { Success = false, Errors = new List<string> { "This sign-in request has expired. Please sign in again." } };

            if (!await VerifyOtpAsync(userId, "Login", code))
            {
                await RecordSecurityEventAsync(userId, "OtpLoginFailed", ipAddress);
                return new AuthResponse
                {
                    Success = false,
                    RequiresTwoFactor = true,
                    Errors = new List<string> { "The code is incorrect or has expired. Request a new code and try again." }
                };
            }

            await _identityService.RemoveUserTokenAsync(userId.ToString(), LoginChallengeTokenName);
            return await CreateLoginResponseAsync(user, rememberMe, ipAddress);
        }

        public async Task<AuthResponse> ResendTwoFactorLoginCodeAsync(int userId, string challengeId)
        {
            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            var activeChallenge = await _identityService.GetUserTokenAsync(userId.ToString(), LoginChallengeTokenName);
            if (user == null || !user.IsActive || !IsSameChallenge(activeChallenge, challengeId))
                return new AuthResponse { Success = false, Errors = new List<string> { "This sign-in request has expired. Please sign in again." } };

            var sent = await SendOtpAsync(user, "Login");
            return new AuthResponse
            {
                Success = sent,
                Message = sent
                    ? $"A new code was sent to {MaskEmail(user.Email)}. It expires in {_loginSecurityOptions.OtpLifetimeMinutes} minutes."
                    : "Please wait a minute before requesting another code. The current code may still be used."
            };
        }

        public async Task<bool> CancelTwoFactorLoginAsync(int userId, string challengeId)
        {
            var activeChallenge = await _identityService.GetUserTokenAsync(userId.ToString(), LoginChallengeTokenName);
            if (!IsSameChallenge(activeChallenge, challengeId))
                return false;

            await _identityService.RemoveUserTokenAsync(userId.ToString(), LoginChallengeTokenName);
            await _identityService.RemoveUserTokenAsync(userId.ToString(), OtpTokenName("Login"));
            return true;
        }

        private async Task<AuthResponse> CreateLoginResponseAsync(IdentityUserDto user, bool rememberMe, string? ipAddress)
        {
            var now = DateTime.UtcNow;
            var absoluteExpiry = rememberMe
                ? now.AddDays(_loginSecurityOptions.RememberStudentDays)
                : now.AddHours(_loginSecurityOptions.AbsoluteSessionHours);
            var refreshExpiry = Min(now.Add(GetIdleTimeout(user.UserType)), absoluteExpiry);
            var token = await _identityService.GenerateJwtTokenAsync(
                user.Id.ToString(), rememberMe, absoluteSessionExpiresAt: absoluteExpiry);
            var refreshToken = _identityService.GenerateRefreshToken();

            var sessionSaved = await _identityService.UpdateRefreshTokenAsync(
                user.Id.ToString(),
                refreshToken,
                refreshExpiry,
                rememberMe,
                absoluteExpiry
            );
            if (!sessionSaved)
                return new AuthResponse { Success = false, Errors = new List<string> { "Unable to create a session." } };
            await RecordSecurityEventAsync(user.Id, "LoginSucceeded", ipAddress);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id.ToString(),
                UserType = user.UserType,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                EmailConfirmed = user.EmailConfirmed,
                RefreshTokenExpiresAt = refreshExpiry.ToString("O")
            };
        }

        private static bool IsSameChallenge(string? expected, string provided) =>
            !string.IsNullOrWhiteSpace(expected)
            && !string.IsNullOrWhiteSpace(provided)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await _identityService.FindUserByRefreshTokenAsync(refreshToken);
            var now = DateTime.UtcNow;
            if (user == null || user.RefreshTokenExpiryTime == null
                || user.RefreshTokenExpiryTime <= now || !user.IsActive
                || user.AbsoluteSessionExpiresAt is not DateTime absoluteExpiry
                || absoluteExpiry <= now)
                return new AuthResponse { Success = false, Errors = new List<string> { "Refresh token is invalid or expired." } };

            var refreshExpiry = Min(now.Add(GetIdleTimeout(user.UserType)), absoluteExpiry);
            var rotatedToken = _identityService.GenerateRefreshToken();
            var updated = await _identityService.RotateRefreshTokenAsync(
                user.Id.ToString(),
                refreshToken,
                rotatedToken,
                refreshExpiry,
                absoluteExpiry,
                user.RememberMe);
            if (!updated)
                return new AuthResponse { Success = false, Errors = new List<string> { "Unable to refresh session." } };

            return new AuthResponse
            {
                Success = true,
                Token = await _identityService.GenerateJwtTokenAsync(
                    user.Id.ToString(), user.RememberMe, absoluteSessionExpiresAt: absoluteExpiry),
                RefreshToken = rotatedToken,
                UserId = user.Id.ToString(),
                UserType = user.UserType,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                EmailConfirmed = user.EmailConfirmed,
                RefreshTokenExpiresAt = refreshExpiry.ToString("O")
            };
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            await _identityService.UpdateRefreshTokenAsync(
                userId.ToString(),
                null,
                DateTime.MinValue // or DateTime.UtcNow depending on semantics
            );
            await _identityService.SignOutAsync();
            return true;
        }

        public async Task<bool> VerifyEmailAsync(int userId, string token)
        {
            var actionToken = await FindActiveActionTokenAsync(userId, token, "RegistrationVerification");
            if (actionToken == null)
                return false;

            if (!await ConsumeActiveActionTokenAsync(actionToken, DateTimeOffset.UtcNow))
                return false;

            var result = await _identityService.ConfirmEmailAddressAsync(userId.ToString());
            if (!result.Succeeded)
                return false;

            await _context.SaveChangesAsync();
            await RecordSecurityEventAsync(userId, "EmailVerified");
            return true;
        }

        public async Task<(bool Success, string? Error)> ResendVerificationAsync(int userId, string? ipAddress)
        {
            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            if (user == null || user.EmailConfirmed)
                return (false, "Verification link cannot be sent for this account.");

            var now = DateTimeOffset.UtcNow;
            var priorRequests = _context.EmailActionTokens
                .Where(token => token.UserId == userId && token.Purpose == "RegistrationVerification");
            if (await priorRequests.CountAsync(token => token.IsResend && token.CreatedAt >= now.AddHours(-1)) >= _emailVerificationOptions.ResendsPerHour
                || await priorRequests.CountAsync(token => token.IsResend && token.CreatedAt >= now.AddDays(-1)) >= _emailVerificationOptions.ResendsPerDay)
            {
                await RecordSecurityEventAsync(userId, "VerificationResendRateLimited", ipAddress);
                return (false, "Verification resend limit reached. Try again later.");
            }

            var activeTokens = await priorRequests
                .Where(token => token.ConsumedAt == null && token.CancelledAt == null)
                .ToListAsync();
            foreach (var activeToken in activeTokens)
                activeToken.CancelledAt = now;

            var tokenValue = await IssueEmailActionTokenAsync(userId, "RegistrationVerification", isResend: true);
            var link = BuildActionLink("verify-email", userId, tokenValue);
            await _emailSender.SendEmailAsync(user.Email, "Verify your Internship Management account", $"Verify your email within {_emailVerificationOptions.LinkExpiryHours} hours:\n{link}");
            await _context.SaveChangesAsync();
            await RecordSecurityEventAsync(userId, "VerificationLinkResent", ipAddress);
            return (true, null);
        }

        public async Task<bool> ConfirmEmailChangeAsync(int userId, string token, string? ipAddress)
        {
            var actionToken = await FindActiveActionTokenAsync(userId, token, "EmailChangeConfirmation");
            if (actionToken?.TargetEmail == null)
                return false;
            var now = DateTimeOffset.UtcNow;
            if (!await ConsumeActiveActionTokenAsync(actionToken, now))
                return false;

            var result = await _identityService.SetEmailAfterConfirmationAsync(userId.ToString(), actionToken.TargetEmail);
            if (!result.Succeeded)
                return false;

            var cancellationTokens = await _context.EmailActionTokens
                .Where(item => item.UserId == userId && item.Purpose == "EmailChangeCancellation"
                    && item.ConsumedAt == null && item.CancelledAt == null)
                .ToListAsync();
            foreach (var cancellationToken in cancellationTokens)
                cancellationToken.CancelledAt = now;

            await _context.SaveChangesAsync();
            await RecordSecurityEventAsync(userId, "EmailChangeConfirmed", ipAddress, actionToken.TargetEmail);
            return true;
        }

        public async Task<bool> CancelEmailChangeAsync(int userId, string token, string? ipAddress)
        {
            var actionToken = await FindActiveActionTokenAsync(userId, token, "EmailChangeCancellation");
            if (actionToken == null)
                return false;

            var now = DateTimeOffset.UtcNow;
            if (!await ConsumeActiveActionTokenAsync(actionToken, now))
                return false;

            var pendingTokens = await _context.EmailActionTokens
                .Where(item => item.UserId == userId && item.Purpose == "EmailChangeConfirmation"
                    && item.ConsumedAt == null && item.CancelledAt == null)
                .ToListAsync();
            foreach (var pendingToken in pendingTokens)
                pendingToken.CancelledAt = now;

            await _context.SaveChangesAsync();
            await RecordSecurityEventAsync(userId, "EmailChangeCancelled", ipAddress);
            return true;
        }

        public async Task RunEmailVerificationHousekeepingAsync(CancellationToken cancellationToken)
        {
            var users = await _identityService.GetAllUsersAsync();
            var now = DateTimeOffset.UtcNow;
            foreach (var user in users.Where(user => !user.EmailConfirmed))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var accountAge = now - new DateTimeOffset(DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc));
                if (accountAge >= TimeSpan.FromDays(_emailVerificationOptions.DeleteAfterDays))
                {
                    var deleteResult = await _identityService.DeleteUserAsync(user.Id.ToString());
                    await RecordSecurityEventAsync(
                        user.Id,
                        deleteResult.Succeeded ? "UnverifiedAccountDeleted" : "UnverifiedAccountDeletionFailed",
                        details: deleteResult.Succeeded ? null : string.Join("; ", deleteResult.Errors));
                    continue;
                }

                if (accountAge < TimeSpan.FromDays(_emailVerificationOptions.ReminderAfterDays)
                    || string.IsNullOrWhiteSpace(user.Email))
                {
                    continue;
                }

                var priorVerificationTokens = await _context.EmailActionTokens
                    .Where(item => item.UserId == user.Id && item.Purpose == "RegistrationVerification")
                    .ToListAsync(cancellationToken);
                if (priorVerificationTokens.Any(item => item.ReminderSentAt != null))
                    continue;

                foreach (var priorToken in priorVerificationTokens.Where(item => item.ConsumedAt == null && item.CancelledAt == null))
                    priorToken.CancelledAt = now;

                var tokenValue = await IssueEmailActionTokenAsync(user.Id, "RegistrationVerification");
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Reminder: verify your Internship Management account",
                    $"Your account is still unverified. Use this link to verify it within {_emailVerificationOptions.LinkExpiryHours} hours:\n{BuildActionLink("verify-email", user.Id, tokenValue)}",
                    cancellationToken);
                foreach (var priorToken in priorVerificationTokens)
                    priorToken.ReminderSentAt = now;
                await _context.SaveChangesAsync(cancellationToken);
                await RecordSecurityEventAsync(user.Id, "VerificationReminderSent");
            }

            var securityEventCutoff = now.AddMonths(-_emailVerificationOptions.SecurityEventRetentionMonths);
            await _context.AccountSecurityEvents
                .Where(item => item.CreatedAt < securityEventCutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _identityService.FindUserByEmailAsync(email);
            if (user == null) return false;

            var token = await _identityService.GeneratePasswordResetTokenAsync(user.Id.ToString());
            var resetUrl = $"{_emailVerificationOptions.PublicWebBaseUrl.TrimEnd('/')}/Account/ResetPassword"
                + $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset your Internship Management password",
                $"A password reset was requested for your account. Choose a new password here:\n\n{resetUrl}\n\nIf you did not request a password reset, you can ignore this email.");
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (await _passwordBreachChecker.IsCompromisedAsync(newPassword))
                return false;

            var user = await _identityService.FindUserByEmailAsync(email);
            if (user == null) return false;

            var result = await _identityService.ResetPasswordAsync(
                user.Id.ToString(),
                token,
                newPassword
            );
            if (result.Succeeded)
            {
                await _identityService.UpdateRefreshTokenAsync(user.Id.ToString(), null, DateTime.UtcNow);
                await _identityService.RemoveUserTokenAsync(user.Id.ToString(), OtpTokenName("Login"));
                await _identityService.RemoveUserTokenAsync(user.Id.ToString(), LoginChallengeTokenName);
            }
            return result.Succeeded;
        }

        public Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordRequest request) =>
            ChangePasswordWithStepUpAsync(userId, request);

        private async Task<IdentityResult> ChangePasswordWithStepUpAsync(int userId, ChangePasswordRequest request)
        {
            if (await _passwordBreachChecker.IsCompromisedAsync(request.NewPassword))
                return IdentityResult.Failure(new List<string> { "Choose a password that has not appeared in known data breaches." });

            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found." });
            if (RequiresEmailOtp(user.UserType)
                && (string.IsNullOrWhiteSpace(request.TwoFactorCode)
                    || !await VerifyOtpAsync(userId, "StepUp", request.TwoFactorCode)))
            {
                return IdentityResult.Failure(new List<string> { "Enter the code we emailed you. Use \"Send me a code\" first if you do not have one." });
            }

            return await _identityService.ChangePasswordAsync(userId.ToString(), request.CurrentPassword, request.NewPassword);
        }

        public async Task<IdentityResult> ChangeEmailAsync(int userId, ChangeEmailRequest request)
        {
            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            if (user == null || !await _identityService.CheckPasswordAsync(user.Email, request.CurrentPassword))
                return IdentityResult.Failure(new List<string> { "Current password is incorrect." });

            if (RequiresEmailOtp(user.UserType)
                && (string.IsNullOrWhiteSpace(request.TwoFactorCode)
                    || !await VerifyOtpAsync(userId, "StepUp", request.TwoFactorCode)))
                return IdentityResult.Failure(new List<string> { "Enter the code we emailed you. Use \"Send me a code\" first if you do not have one." });

            var existingUser = await _identityService.FindUserByEmailAsync(request.NewEmail);
            if (existingUser != null)
                return IdentityResult.Failure(new List<string> { "That email address is already in use." });

            var now = DateTimeOffset.UtcNow;
            var outstanding = await _context.EmailActionTokens
                .Where(item => item.UserId == userId && item.Purpose.StartsWith("EmailChange")
                    && item.ConsumedAt == null && item.CancelledAt == null)
                .ToListAsync();
            foreach (var item in outstanding)
                item.CancelledAt = now;

            var confirmToken = await IssueEmailActionTokenAsync(userId, "EmailChangeConfirmation", request.NewEmail);
            var cancelToken = await IssueEmailActionTokenAsync(userId, "EmailChangeCancellation", request.NewEmail);
            await _emailSender.SendEmailAsync(
                request.NewEmail,
                "Confirm your new Internship Management email",
                $"Confirm this email address within {_emailVerificationOptions.LinkExpiryHours} hours:\n{BuildActionLink("confirm-email-change", userId, confirmToken)}");
            await _emailSender.SendEmailAsync(
                user.Email,
                "Email change requested",
                $"A request was made to change your account email. Cancel it if this was not you:\n{BuildActionLink("cancel-email-change", userId, cancelToken)}");

            await _context.SaveChangesAsync();
            await RecordSecurityEventAsync(userId, "EmailChangeRequested", details: request.NewEmail);
            return IdentityResult.Success(userId.ToString());
        }

        public async Task<AuthResponse> SendSecurityCodeAsync(int userId)
        {
            var user = await _identityService.FindUserByIdAsync(userId.ToString());
            if (user == null || !RequiresEmailOtp(user.UserType))
                return new AuthResponse { Success = false, Errors = new List<string> { "A code is not required for this account." } };

            var sent = await SendOtpAsync(user, "StepUp");
            return new AuthResponse
            {
                Success = sent,
                Message = sent
                    ? $"We emailed a 6-digit code to {MaskEmail(user.Email)}."
                    : "A code was sent a moment ago. Please wait a minute before asking for another."
            };
        }

        // ---- Emailed one-time codes -------------------------------------------------------------------------
        // Stored (hashed, single use, expiring, attempt-limited) in the Identity user-tokens table, so no migration is needed.
        // Token value: salt|hash|expiryTicks|attempts|issuedTicks

        private static string OtpTokenName(string purpose) => $"EmailOtp:{purpose}";

        private static string HashOtp(int userId, string purpose, string salt, string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}|{purpose}|{salt}|{code}"));
            return Convert.ToBase64String(bytes);
        }

        private async Task<bool> SendOtpAsync(IdentityUserDto user, string purpose)
        {
            var name = OtpTokenName(purpose);
            var existing = await _identityService.GetUserTokenAsync(user.Id.ToString(), name);
            var now = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(existing))
            {
                var parts = existing.Split('|');
                if (parts.Length == 5
                    && long.TryParse(parts[4], out var issuedTicks)
                    && now - new DateTime(issuedTicks, DateTimeKind.Utc) < TimeSpan.FromSeconds(_loginSecurityOptions.OtpResendSeconds)
                    && long.TryParse(parts[2], out var existingExpiry)
                    && existingExpiry > now.Ticks)
                    return false;
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var expiry = now.AddMinutes(_loginSecurityOptions.OtpLifetimeMinutes);
            await _identityService.SetUserTokenAsync(user.Id.ToString(), name,
                $"{salt}|{HashOtp(user.Id, purpose, salt, code)}|{expiry.Ticks}|0|{now.Ticks}");

            var reason = purpose == "Login" ? "sign in" : "confirm this change to your account";
            await _emailSender.SendEmailAsync(
                user.Email,
                "Your Internship Management security code",
                $"Your code to {reason} is {code}. It expires in {_loginSecurityOptions.OtpLifetimeMinutes} minutes and can be used once. " +
                "If you did not request it, someone may know your password - please change it and contact support.");
            await RecordSecurityEventAsync(user.Id, "OtpIssued");
            return true;
        }

        private async Task<bool> VerifyOtpAsync(int userId, string purpose, string code)
        {
            var name = OtpTokenName(purpose);
            var stored = await _identityService.GetUserTokenAsync(userId.ToString(), name);
            if (string.IsNullOrEmpty(stored)) return false;

            var parts = stored.Split('|');
            if (parts.Length != 5
                || !long.TryParse(parts[2], out var expiryTicks)
                || !int.TryParse(parts[3], out var attempts))
            {
                await _identityService.RemoveUserTokenAsync(userId.ToString(), name);
                return false;
            }

            if (expiryTicks <= DateTime.UtcNow.Ticks || attempts >= _loginSecurityOptions.OtpMaxAttempts)
            {
                await _identityService.RemoveUserTokenAsync(userId.ToString(), name);
                return false;
            }

            var candidate = HashOtp(userId, purpose, parts[0], code.Trim().Replace(" ", string.Empty));
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(parts[1]));

            if (matches)
            {
                await _identityService.RemoveUserTokenAsync(userId.ToString(), name);
                return true;
            }

            await _identityService.SetUserTokenAsync(userId.ToString(), name,
                $"{parts[0]}|{parts[1]}|{parts[2]}|{attempts + 1}|{parts[4]}");
            return false;
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return email;
            return email[0] + new string('*', Math.Min(at - 1, 6)) + email[at..];
        }

        public Task<IdentityResult> UpdateProfilePictureUrlAsync(int userId, string profilePictureUrl) =>
            _identityService.UpdateProfilePictureUrlAsync(userId.ToString(), profilePictureUrl);

        public Task<AccountPreferencesDto> GetAccountPreferencesAsync(int userId) =>
            _identityService.GetAccountPreferencesAsync(userId.ToString());

        public Task<IdentityResult> UpdateAccountPreferencesAsync(int userId, UpdateAccountPreferencesRequest request) =>
            _identityService.UpdateAccountPreferencesAsync(userId.ToString(), new AccountPreferencesDto
            {
                EmailNotifications = request.EmailNotifications,
                InternshipAlerts = request.InternshipAlerts
            });

        public Task<IdentityResult> DeactivateAccountAsync(int userId) =>
            _identityService.DeactivateAccountAsync(userId.ToString());

        private async Task<string> IssueEmailActionTokenAsync(
            int userId, string purpose, string? targetEmail = null, bool isResend = false)
        {
            var now = DateTimeOffset.UtcNow;
            var existingTokens = await _context.EmailActionTokens
                .Where(item => item.UserId == userId && item.Purpose == purpose
                    && item.ConsumedAt == null && item.CancelledAt == null)
                .ToListAsync();
            foreach (var item in existingTokens)
                item.CancelledAt = now;

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            _context.EmailActionTokens.Add(new InternshipManagement.Domain.Entities.EmailActionToken
            {
                TokenHash = HashToken(rawToken),
                UserId = userId,
                Purpose = purpose,
                TargetEmail = targetEmail,
                IsResend = isResend,
                CreatedAt = now,
                ExpiresAt = now.AddHours(_emailVerificationOptions.LinkExpiryHours)
            });
            await _context.SaveChangesAsync();
            return rawToken;
        }

        private async Task<InternshipManagement.Domain.Entities.EmailActionToken?> FindActiveActionTokenAsync(
            int userId, string rawToken, string purpose)
        {
            var tokenHash = HashToken(rawToken);
            return await _context.EmailActionTokens.FirstOrDefaultAsync(item =>
                item.UserId == userId && item.TokenHash == tokenHash && item.Purpose == purpose
                && item.ExpiresAt > DateTimeOffset.UtcNow && item.ConsumedAt == null && item.CancelledAt == null);
        }

        private async Task<bool> ConsumeActiveActionTokenAsync(
            InternshipManagement.Domain.Entities.EmailActionToken actionToken,
            DateTimeOffset consumedAt)
        {
            var updatedRows = await _context.EmailActionTokens
                .Where(item => item.TokenHash == actionToken.TokenHash
                    && item.UserId == actionToken.UserId
                    && item.Purpose == actionToken.Purpose
                    && item.ExpiresAt > consumedAt
                    && item.ConsumedAt == null
                    && item.CancelledAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ConsumedAt, consumedAt));
            return updatedRows == 1;
        }

        private string BuildActionLink(string action, int userId, string token)
        {
            var baseUrl = _emailVerificationOptions.PublicApiBaseUrl.TrimEnd('/');
            return $"{baseUrl}/api/Auth/{action}?userId={userId}&token={Uri.EscapeDataString(token)}";
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private async Task RecordSecurityEventAsync(int? userId, string eventType, string? ipAddress = null, string? details = null)
        {
            _context.AccountSecurityEvents.Add(new InternshipManagement.Domain.Entities.AccountSecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                IpAddress = ipAddress,
                Details = details,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Helper methods
        private static string GenerateJwtToken(string userId, string email, string userType)
        {
            // This should be in IdentityService but kept here for completeness
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim("userId", userId),
                new Claim("userType", userType)
            };

            // In real implementation, this would use configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHere1234567890!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "InternshipManagementAPI",
                audience: "InternshipManagementClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static bool RequiresEmailOtp(string userType) =>
            userType.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || userType.Equals("Company", StringComparison.OrdinalIgnoreCase);

        private TimeSpan GetIdleTimeout(string userType) =>
            userType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromHours(_loginSecurityOptions.StudentIdleTimeoutHours)
                : TimeSpan.FromMinutes(_loginSecurityOptions.AdminCompanyIdleTimeoutMinutes);

        private static DateTime Min(DateTime first, DateTime second) =>
            first <= second ? first : second;

        private async Task NotifyOtherAdminsAsync(int excludedUserId, string message)
        {
            var administrators = await _identityService.GetAllUsersAsync();
            foreach (var administrator in administrators.Where(user =>
                user.Id != excludedUserId
                && user.IsActive
                && user.UserType.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                await _emailSender.SendEmailAsync(
                    administrator.Email,
                    "Administrator security alert",
                    message);
            }
        }
    }
}
