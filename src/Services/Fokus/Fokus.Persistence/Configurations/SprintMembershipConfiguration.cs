using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fokus.Persistence.Configurations;

public class SprintMembershipConfiguration : IEntityTypeConfiguration<SprintMembership>
{
    public void Configure(EntityTypeBuilder<SprintMembership> builder)
    {
        builder.HasKey(sm => new { sm.SprintId, sm.TicketKey });

        builder.Property(sm => sm.TicketKey).HasMaxLength(64);
        builder.Property(sm => sm.FinalStatus).IsRequired().HasMaxLength(128);
        builder.Property(sm => sm.StoryPoints).HasColumnType("decimal(8,2)");

        builder.HasOne(sm => sm.Sprint)
            .WithMany(s => s.Memberships)
            .HasForeignKey(sm => sm.SprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sm => sm.Ticket)
            .WithMany()
            .HasForeignKey(sm => sm.TicketKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sm => sm.TicketKey).HasDatabaseName("IX_SprintMembership_TicketKey");
    }
}
