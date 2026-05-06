using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fokus.Persistence.Configurations;

public class StatusTransitionConfiguration : IEntityTypeConfiguration<StatusTransition>
{
    public void Configure(EntityTypeBuilder<StatusTransition> builder)
    {
        builder.HasKey(st => st.Id);
        builder.Property(st => st.Id).ValueGeneratedOnAdd();

        builder.Property(st => st.TicketKey).IsRequired().HasMaxLength(64);
        builder.Property(st => st.FromStatus).IsRequired().HasMaxLength(128);
        builder.Property(st => st.ToStatus).IsRequired().HasMaxLength(128);
        builder.Property(st => st.AuthorId).HasMaxLength(128);

        builder.HasOne(st => st.Ticket)
            .WithMany(t => t.StatusTransitions)
            .HasForeignKey(st => st.TicketKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(st => new { st.TicketKey, st.Timestamp })
            .HasDatabaseName("IX_StatusTransition_TicketKey_Timestamp");
    }
}
