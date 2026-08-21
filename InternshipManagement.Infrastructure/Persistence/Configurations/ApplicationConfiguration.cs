using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<InternshipApplication>
    {
        public void Configure(EntityTypeBuilder<InternshipApplication> builder)
        {
            builder.ToTable("InternshipApplications");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.CoverLetter)
                .HasMaxLength(2000);

            builder.Property(a => a.AdditionalDocuments)
                .HasColumnType("nvarchar(max)");

            builder.Property(a => a.Status)
                .HasDefaultValue(ApplicationStatus.Applied);

            builder.Property(a => a.StatusNotes)
                .HasMaxLength(500);

            builder.Property(a => a.ShortlistNotes)
                .HasMaxLength(500);

            builder.HasOne(a => a.StudentProfile)
                .WithMany(s => s.InternshipApplications)
                .HasForeignKey(a => a.StudentProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Internship)
                .WithMany(i => i.InternshipApplications)
                .HasForeignKey(a => a.InternshipId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.StudentProfileId, a.InternshipId })
                .IsUnique()
                .HasDatabaseName("IX_InternshipApplication_Student_Internship_Unique");

            builder.HasIndex(a => a.StudentProfileId);
            builder.HasIndex(a => a.InternshipId);
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => a.IsShortlisted);
            builder.HasIndex(a => a.CreatedAt);
        }
    }
}