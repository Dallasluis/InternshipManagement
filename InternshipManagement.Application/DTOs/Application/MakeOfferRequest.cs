using System.ComponentModel.DataAnnotations;

namespace InternshipManagement.Application.DTOs.Application
{
    public class MakeOfferRequest
    {
        [Range(0, double.MaxValue)]
        public decimal? StipendAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public string? OfferDetails { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
