using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Application
{
    public class RespondToOfferRequest
    {
        [Required]
        public bool Accepted { get; set; }
    }
}
