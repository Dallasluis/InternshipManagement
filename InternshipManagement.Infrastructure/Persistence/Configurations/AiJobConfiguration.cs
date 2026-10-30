using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class AiJobConfiguration : IEntityTypeConfiguration<AiJob>
    {
        public void Configure(EntityTypeBuilder<AiJob> builder)
        {
            builder.ToTable("AiJobs");
            builder.HasKey(j => j.Id);

            builder.Property(j => j.JobType).IsRequired().HasMaxLength(50);
            builder.Property(j => j.Status).IsRequired().HasMaxLength(20);
            builder.Property(j => j.ResultJson).HasColumnType("nvarchar(max)");
            builder.Property(j => j.Error).HasMaxLength(500);

            // "does this user already have a running job of this type?"
            builder.HasIndex(j => new { j.UserId, j.JobType, j.Status });

            // the clean-up of old jobs
            builder.HasIndex(j => j.CreatedAt);
        }
    }
}
