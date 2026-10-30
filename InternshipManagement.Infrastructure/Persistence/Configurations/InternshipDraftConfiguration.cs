using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class InternshipDraftConfiguration : IEntityTypeConfiguration<InternshipDraft>
    {
        public void Configure(EntityTypeBuilder<InternshipDraft> builder)
        {
            builder.ToTable("InternshipDrafts");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
            builder.Property(d => d.Prompt).HasMaxLength(4000);
            builder.Property(d => d.DraftJson).IsRequired().HasColumnType("nvarchar(max)");

            builder.HasOne(d => d.CompanyProfile)
                .WithMany()
                .HasForeignKey(d => d.CompanyProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.CompanyProfileId);
        }
    }
}
