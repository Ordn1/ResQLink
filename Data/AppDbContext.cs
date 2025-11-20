using Microsoft.EntityFrameworkCore;
using ResQLink.Data.Entities;

namespace ResQLink.Data;

public partial class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Username).IsUnique();

            e.Property(x => x.Username).HasMaxLength(56).IsRequired();
            e.Property(x => x.Password).HasMaxLength(56).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoleId);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
        });

        base.OnModelCreating(modelBuilder);
    }
}