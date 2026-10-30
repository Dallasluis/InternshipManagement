using InternshipManagement.Domain.Entities;
using Xunit;

namespace InternshipManagement.UnitTests;

public class StudentStructuredProfileTests
{
    [Fact]
    public void StudentProfile_Stores_Structured_Profile_Collections()
    {
        var profile = new StudentProfile
        {
            FirstName = "Aina",
            LastName = "Nghipandulwa",
            ResumeUrl = "/uploads/resumes/current.pdf",
            Education = new List<Education>
            {
                new()
                {
                    Institution = "NUST",
                    Degree = "Bachelor",
                    FieldOfStudy = "Computer Science",
                    CompletionStatus = "InProgress",
                    SupportingDocumentUrl = "/uploads/qualifications/transcript.pdf"
                }
            },
            Skills = new List<Skill>
            {
                new() { Name = "ASP.NET Core", ProficiencyLevel = "Intermediate" }
            },
            WorkExperience = new List<WorkExperience>
            {
                new() { Company = "Example Co", Position = "Developer Intern" }
            },
            Certifications = new List<StudentCertification>
            {
                new()
                {
                    Name = "Azure Fundamentals",
                    IssuingOrganization = "Microsoft",
                    CredentialUrl = "https://example.com/credential",
                    CertificateFileUrl = "/uploads/certificates/azure.pdf"
                }
            },
            Projects = new List<StudentProject>
            {
                new()
                {
                    Title = "Internship Tracker",
                    Technologies = "ASP.NET Core, SQL Server",
                    RepositoryUrl = "https://example.com/repo"
                }
            }
        };

        Assert.Single(profile.Education);
        Assert.Single(profile.Skills);
        Assert.Single(profile.WorkExperience);
        Assert.Single(profile.Certifications);
        Assert.Single(profile.Projects);
        Assert.Equal("/uploads/resumes/current.pdf", profile.ResumeUrl);
        Assert.Equal("InProgress", profile.Education.Single().CompletionStatus);
        Assert.Equal("Azure Fundamentals", profile.Certifications.Single().Name);
        Assert.Equal("Internship Tracker", profile.Projects.Single().Title);
    }
}
