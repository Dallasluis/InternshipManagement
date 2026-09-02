using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Web.Models.Application
{
    public class MakeOfferRequest
    {
        public decimal? StipendAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public string? OfferDetails { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
