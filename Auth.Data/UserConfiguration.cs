using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Data;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.RoleId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasData(
    new User
    {
        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Name = "AdminMariam",
        Email = "Madmin@gmail.com",
        PasswordHash = "$2a$11$VvfcTjelJxivm0XAL1GAK.4HIO24j.tkE3wCGH8KZTeG2OoYPSW2K",
        RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CreatedAtUtc = new DateTime(
            2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAtUtc = null,
        IsDeleted = false
    });
    }
}