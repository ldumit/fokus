using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fokus.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Key);
        builder.Property(t => t.Key).HasMaxLength(64);

        builder.Property(t => t.Summary).IsRequired().HasMaxLength(1024);
        builder.Property(t => t.IssueType).IsRequired().HasMaxLength(64);
        builder.Property(t => t.EpicKey).HasMaxLength(64);
        builder.Property(t => t.EpicName).HasMaxLength(256);
        builder.Property(t => t.AssigneeId).HasMaxLength(128);
        builder.Property(t => t.Priority).IsRequired().HasMaxLength(64);
        builder.Property(t => t.CurrentStatus).IsRequired().HasMaxLength(128);
        builder.Property(t => t.StoryPoints).HasColumnType("decimal(8,2)");

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.EpicKey).HasDatabaseName("IX_Ticket_EpicKey");
        builder.HasIndex(t => t.AssigneeId).HasDatabaseName("IX_Ticket_AssigneeId");
    }
}
