using System.Text.Json.Serialization;

namespace InternshipManagement.Web.Models.Company
{
    public class UpdateCompanyProfileRequest
    {
        public string? LegalName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LogoUrl { get; set; }
        [JsonIgnore]
        public IFormFile? LogoFile { get; set; }
    }
}