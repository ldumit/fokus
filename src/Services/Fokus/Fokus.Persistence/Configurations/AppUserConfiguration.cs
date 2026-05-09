namespace Fokus.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.GoogleId).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.GoogleId).IsUnique().HasDatabaseName("IX_AppUsers_GoogleId");

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_AppUsers_Email");

        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(u => u.AvatarUrl).HasMaxLength(1024);
        builder.Property(u => u.Role).IsRequired().HasMaxLength(32).HasDefaultValue("Manager");
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
    }
}
