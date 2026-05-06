using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fokus.Persistence.Configurations;

public class DeveloperConfiguration : IEntityTypeConfiguration<Developer>
{
    public void Configure(EntityTypeBuilder<Developer> builder)
    {
        builder.HasKey(d => d.AccountId);
        builder.Property(d => d.AccountId).HasMaxLength(128);

        builder.Property(d => d.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(d => d.AvatarUrl).HasMaxLength(1024);
        builder.Property(d => d.SubTeam).HasMaxLength(128);
    }
}
