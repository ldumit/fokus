namespace Fokus.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(i => i.Email).HasDatabaseName("IX_Invitations_Email");

        builder.Property(i => i.Role).IsRequired().HasMaxLength(32).HasDefaultValue("Manager");

        builder.Property(i => i.Token).IsRequired().HasMaxLength(128);
        builder.HasIndex(i => i.Token).IsUnique().HasDatabaseName("IX_Invitations_Token");

        builder.Property(i => i.Status).IsRequired().HasMaxLength(32).HasDefaultValue("Pending");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
