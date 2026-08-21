using System.Security.Claims;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<IdentityResult> CreateUserAsync(string email, string password, string firstName, string lastName, string userType);
        Task<IdentityUserDto> FindUserByEmailAsync(string email);
        Task<IdentityUserDto> FindUserByIdAsync(string userId);
        Task<bool> CheckPasswordAsync(string email, string password);
        Task<SignInResult> SignInAsync(string email, string password, bool isPersistent);
        Task SignOutAsync();
        Task<bool> IsInRoleAsync(string userId, string role);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<IdentityResult> AddToRoleAsync(string userId, string role);
        Task<bool> RoleExistsAsync(string role);
        Task<IdentityResult> CreateRoleAsync(string role);
        Task<string> GenerateEmailConfirmationTokenAsync(string userId);
        Task<IdentityResult> ConfirmEmailAsync(string userId, string token);
        Task<string> GeneratePasswordResetTokenAsync(string userId);
        Task<IdentityResult> ResetPasswordAsync(string userId, string token, string newPassword);
        Task<string> GenerateJwtTokenAsync(string userId);
        string GenerateRefreshToken();
        Task<bool> UpdateRefreshTokenAsync(string userId, string? refreshToken, DateTime? expiryTime);
        Task<ClaimsPrincipal> GetUserPrincipalAsync();
        Task<IdentityUserDto> GetCurrentUserAsync();
    }

    public class IdentityResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();
        public string UserId { get; set; }

        public static IdentityResult Success(string userId = null) =>
            new() { Succeeded = true, UserId = userId };

        public static IdentityResult Failure(List<string> errors) =>
            new() { Succeeded = false, Errors = errors };
    }

    public class SignInResult
    {
        public bool Succeeded { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsNotAllowed { get; set; }
        public bool RequiresTwoFactor { get; set; }

        public static SignInResult Success() => new() { Succeeded = true };
        public static SignInResult Failed() => new() { Succeeded = false };
        public static SignInResult LockedOut() => new() { Succeeded = false, IsLockedOut = true };
        public static SignInResult NotAllowed() => new() { Succeeded = false, IsNotAllowed = true };
        public static SignInResult TwoFactorRequired() => new() { Succeeded = false, RequiresTwoFactor = true };
    }

    public class IdentityUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserType { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}