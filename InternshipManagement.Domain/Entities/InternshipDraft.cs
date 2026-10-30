using InternshipManagement.Domain.Common;

namespace InternshipManagement.Domain.Entities
{
	/// <summary>
	/// A saved, unpublished posting draft (usually AI-generated). It is NOT an Internship: it never
	/// appears in listings or matching. The company edits it, then submits it through the normal
	/// create-internship endpoint, which is the only thing that writes to Internships.
	/// </summary>
	public class InternshipDraft : BaseEntity
	{
		public int CompanyProfileId { get; set; }
		public virtual CompanyProfile CompanyProfile { get; set; } = null!;

		/// <summary>Copy of the draft title, so lists don't have to parse the JSON.</summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>The description the company typed for the AI.</summary>
		public string? Prompt { get; set; }

		/// <summary>The draft fields as JSON (same shape as the form), so form changes need no migration.</summary>
		public string DraftJson { get; set; } = "{}";
	}
}
