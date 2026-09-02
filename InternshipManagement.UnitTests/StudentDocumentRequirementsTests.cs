using InternshipManagement.Domain.Entities;
using Xunit;

namespace InternshipManagement.UnitTests;

public class StudentDocumentRequirementsTests
{
    [Fact]
    public void StudentProfile_Stores_All_Internship_Application_Documents()
    {
        var profile = new StudentProfile
        {
            ResumeUrl = "/uploads/resumes/sample.pdf",
            CoverLetterUrl = "/uploads/cover-letters/sample.pdf",
            AcademicTranscriptUrl = "/uploads/transcripts/sample.pdf",
            QualificationDocumentUrl = "/uploads/qualifications/sample.pdf",
            IdentificationDocumentUrl = "/uploads/ids/sample.pdf",
            PortfolioUrl = "/uploads/portfolio/sample.pdf",
            CertificatesUrl = "/uploads/certificates/sample.pdf",
            OtherSupportingDocumentsUrl = "/uploads/supporting/sample.pdf"
        };

        Assert.Equal("/uploads/resumes/sample.pdf", profile.ResumeUrl);
        Assert.Equal("/uploads/cover-letters/sample.pdf", profile.CoverLetterUrl);
        Assert.Equal("/uploads/transcripts/sample.pdf", profile.AcademicTranscriptUrl);
        Assert.Equal("/uploads/qualifications/sample.pdf", profile.QualificationDocumentUrl);
        Assert.Equal("/uploads/ids/sample.pdf", profile.IdentificationDocumentUrl);
        Assert.Equal("/uploads/portfolio/sample.pdf", profile.PortfolioUrl);
        Assert.Equal("/uploads/certificates/sample.pdf", profile.CertificatesUrl);
        Assert.Equal("/uploads/supporting/sample.pdf", profile.OtherSupportingDocumentsUrl);
    }
}
