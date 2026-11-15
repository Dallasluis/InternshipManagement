namespace InternshipManagement.Web.Models.Offer
{
    public class OfferLetterModel
    {
        public int ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string InternshipTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal? StipendAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Details { get; set; }
        public DateTime? SentAt { get; set; }
        public string? ContractFileName { get; set; }
        public long? ContractLength { get; set; }
        public bool CanAttachContract { get; set; }
    }
}
