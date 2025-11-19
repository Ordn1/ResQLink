using Microsoft.EntityFrameworkCore;
using ResQLink.Data.Entities;

namespace ResQLink.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User entity configuration
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");

            e.HasKey(x => x.Id);

            e.HasIndex(x => x.Username)
             .IsUnique();

            e.Property(x => x.Username)
             .HasMaxLength(64)
             .IsRequired();

            e.Property(x => x.Role)
             .HasMaxLength(32)
             .IsRequired();

            e.Property(x => x.PasswordHash)
             .IsRequired();

            e.Property(x => x.PasswordSalt)
             .IsRequired();

            e.Property(x => x.Email)
             .HasMaxLength(256);

            e.Property(x => x.CreatedUtc)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        base.OnModelCreating(modelBuilder);
    }
}