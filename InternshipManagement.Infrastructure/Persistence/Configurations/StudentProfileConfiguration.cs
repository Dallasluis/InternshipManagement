using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InternshipManagement.Domain.Entities;

namespace InternshipManagement.Domain.Configurations
{
    public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
    {
        public void Configure(EntityTypeBuilder<StudentProfile> builder)
        {
            builder.ToTable("StudentProfiles");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Bio)
                .HasMaxLength(1000);

            builder.Property(s => s.Location)
                .HasMaxLength(200);

            builder.Property(s => s.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(s => s.LinkedInUrl)
                .HasMaxLength(255);

            builder.Property(s => s.PortfolioUrl)
                .HasMaxLength(255);

            builder.Property(s => s.University)
                .HasMaxLength(200);

            builder.Property(s => s.Programme)
                .HasMaxLength(200);

            builder.Property(s => s.YearOfStudy)
                .HasMaxLength(50);

            builder.Property(s => s.ExpectedGraduationYear)
                .HasMaxLength(10);

            builder.Property(s => s.ResumeUrl)
                .HasMaxLength(500);

            builder.Property(s => s.CoverLetterUrl)
                .HasMaxLength(500);

            builder.Property(s => s.AcademicTranscriptUrl)
                .HasMaxLength(500);

            builder.Property(s => s.QualificationDocumentUrl)
                .HasMaxLength(500);

            builder.Property(s => s.IdentificationDocumentUrl)
                .HasMaxLength(500);

            builder.Property(s => s.CertificatesUrl)
                .HasMaxLength(500);

            builder.Property(s => s.OtherSupportingDocumentsUrl)
                .HasMaxLength(500);

            builder.Property(s => s.ProfilePictureUrl)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(s => s.UserId).IsUnique();
            builder.HasIndex(s => s.Programme);
            builder.HasIndex(s => s.University);
            builder.HasIndex(s => s.Location);
        }
    }
}