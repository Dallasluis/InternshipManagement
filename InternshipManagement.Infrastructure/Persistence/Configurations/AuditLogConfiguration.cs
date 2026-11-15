using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.UserEmail).HasMaxLength(256);
            builder.Property(a => a.Role).IsRequired().HasMaxLength(30);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(150);
            builder.Property(a => a.HttpMethod).IsRequired().HasMaxLength(10);
            builder.Property(a => a.Path).IsRequired().HasMaxLength(300);
            builder.Property(a => a.EntityId).HasMaxLength(50);
            builder.Property(a => a.IpAddress).HasMaxLength(64);
            builder.HasIndex(a => a.CreatedAt);
            builder.HasIndex(a => new { a.UserId, a.CreatedAt });
            builder.HasIndex(a => new { a.Action, a.CreatedAt });
        }
    }
}
