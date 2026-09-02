using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Application
{
    public class RespondToOfferRequest
    {
        [Required]
        public bool Accepted { get; set; }
    }
}
