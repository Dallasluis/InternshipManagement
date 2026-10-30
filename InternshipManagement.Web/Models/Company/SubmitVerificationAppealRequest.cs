using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Company
{
    public sealed class SubmitVerificationAppealRequest
    {
        [Required, MinLength(20), MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;
    }
}
