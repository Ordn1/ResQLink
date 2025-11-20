using Microsoft.EntityFrameworkCore;
using ResQLink.Data.Entities;

namespace ResQLink.Data;

public partial class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Disaster> Disasters => Set<Disaster>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
    public DbSet<Evacuee> Evacuees => Set<Evacuee>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<ReliefGood> ReliefGoods => Set<ReliefGood>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ReliefGoodCategory> ReliefGoodCategories => Set<ReliefGoodCategory>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<ResourceAllocation> ResourceAllocations => Set<ResourceAllocation>();
    public DbSet<ResourceDistribution> ResourceDistributions => Set<ResourceDistribution>();
    public DbSet<ReportDisasterSummary> ReportDisasterSummaries => Set<ReportDisasterSummary>();
    public DbSet<ReportResourceDistribution> ReportResourceDistributions => Set<ReportResourceDistribution>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Username).HasMaxLength(56).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // UserRoles
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(r => r.RoleId);
            e.HasIndex(r => r.RoleName).IsUnique();
            e.Property(r => r.RoleName).HasMaxLength(50).IsRequired();
            e.Property(r => r.Description).HasMaxLength(255);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // UserProfiles
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("UserProfiles");
            e.HasKey(p => p.UserProfileId);
            e.HasIndex(p => p.UserId).IsUnique();
            e.HasOne(p => p.User)
             .WithOne()
             .HasForeignKey<UserProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Disasters
        modelBuilder.Entity<Disaster>(e =>
        {
            e.ToTable("Disasters");
            e.HasKey(d => d.DisasterId);
            e.Property(d => d.Title).HasMaxLength(255).IsRequired();
            e.Property(d => d.DisasterType).HasMaxLength(100).IsRequired();
            e.Property(d => d.Severity).HasMaxLength(50).IsRequired();
            e.Property(d => d.Status).HasMaxLength(50).IsRequired();
            e.Property(d => d.Location).HasMaxLength(255).IsRequired();
            e.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Shelters
        modelBuilder.Entity<Shelter>(e =>
        {
            e.ToTable("Shelters");
            e.HasKey(s => s.ShelterId);
            e.Property(s => s.Name).HasMaxLength(255).IsRequired();
            e.Property(s => s.Location).HasMaxLength(255);
            e.HasOne(s => s.Disaster)
             .WithMany(d => d.Shelters)
             .HasForeignKey(s => s.DisasterId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // Evacuees
        modelBuilder.Entity<Evacuee>(e =>
        {
            e.ToTable("Evacuees");
            e.HasKey(ev => ev.EvacueeId);
            e.Property(ev => ev.FirstName).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.LastName).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.Status).HasMaxLength(50).IsRequired();
            e.HasOne(ev => ev.Disaster)
             .WithMany(d => d.Evacuees)
             .HasForeignKey(ev => ev.DisasterId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ev => ev.Shelter)
             .WithMany(s => s.Evacuees)
             .HasForeignKey(ev => ev.ShelterId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // Donors
        modelBuilder.Entity<Donor>(e =>
        {
            e.ToTable("Donors");
            e.HasKey(d => d.DonorId);
            e.Property(d => d.Name).HasMaxLength(255).IsRequired();
            e.Property(d => d.Email).HasMaxLength(255);
            e.Property(d => d.ContactNumber).HasMaxLength(30);
            e.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ReliefGoods
        modelBuilder.Entity<ReliefGood>(e =>
        {
            e.ToTable("Relief_Goods");
            e.HasKey(r => r.RgId);
            e.Property(r => r.Name).HasMaxLength(255).IsRequired();
            e.Property(r => r.Unit).HasMaxLength(50).IsRequired();
            e.Property(r => r.Description).HasMaxLength(255);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Categories
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(255);
            e.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ReliefGoodCategories (pivot)
        modelBuilder.Entity<ReliefGoodCategory>(e =>
        {
            e.ToTable("Relief_Goods_Categories");
            e.HasKey(x => new { x.RgId, x.CategoryId });
            e.HasOne(x => x.ReliefGood)
             .WithMany(r => r.Categories)
             .HasForeignKey(x => x.RgId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
             .WithMany(c => c.ReliefGoods)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Stocks
        modelBuilder.Entity<Stock>(e =>
        {
            e.ToTable("Stocks");
            e.HasKey(s => s.StockId);
            e.Property(s => s.Location).HasMaxLength(255);
            e.Property(s => s.LastUpdated).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(s => s.ReliefGood)
             .WithMany(r => r.Stocks)
             .HasForeignKey(s => s.RgId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Disaster)
             .WithMany()
             .HasForeignKey(s => s.DisasterId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(s => s.Shelter)
             .WithMany()
             .HasForeignKey(s => s.ShelterId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // Donations
        modelBuilder.Entity<Donation>(e =>
        {
            e.ToTable("Donations");
            e.HasKey(d => d.DonationId);
            e.Property(d => d.Amount).HasColumnType("decimal(12,2)");
            e.Property(d => d.DonationType).HasMaxLength(100).IsRequired();
            e.Property(d => d.Status).HasMaxLength(50).IsRequired();
            e.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(d => d.Donor)
             .WithMany(donor => donor.Donations)
             .HasForeignKey(d => d.DonorId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.RecordedBy)
             .WithMany()
             .HasForeignKey(d => d.RecordedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Disaster)
             .WithMany()
             .HasForeignKey(d => d.DisasterId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ResourceAllocations
        modelBuilder.Entity<ResourceAllocation>(e =>
        {
            e.ToTable("ResourceAllocations");
            e.HasKey(a => a.AllocationId);
            e.Property(a => a.AllocatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(a => a.Stock)
             .WithMany(s => s.Allocations)
             .HasForeignKey(a => a.StockId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Shelter)
             .WithMany()
             .HasForeignKey(a => a.ShelterId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.AllocatedBy)
             .WithMany()
             .HasForeignKey(a => a.AllocatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ResourceDistributions
        modelBuilder.Entity<ResourceDistribution>(e =>
        {
            e.ToTable("ResourceDistributions");
            e.HasKey(d => d.DistributionId);
            e.Property(d => d.DistributedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(d => d.Allocation)
             .WithMany(a => a.Distributions)
             .HasForeignKey(d => d.AllocationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Evacuee)
             .WithMany(e => e.Distributions)
             .HasForeignKey(d => d.EvacueeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.DistributedBy)
             .WithMany()
             .HasForeignKey(d => d.DistributedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Reports
        modelBuilder.Entity<ReportDisasterSummary>(e =>
        {
            e.ToTable("ReportDisasterSummary");
            e.HasKey(r => r.ReportId);
            e.Property(r => r.GeneratedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(r => r.Disaster)
             .WithMany()
             .HasForeignKey(r => r.DisasterId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReportResourceDistribution>(e =>
        {
            e.ToTable("ReportResourceDistribution");
            e.HasKey(r => r.ReportId);
            e.Property(r => r.DistributedItems).IsRequired();
            e.Property(r => r.GeneratedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(r => r.Disaster)
             .WithMany()
             .HasForeignKey(r => r.DisasterId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Shelter)
             .WithMany()
             .HasForeignKey(r => r.ShelterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLogs
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(a => a.LogId);
            e.Property(a => a.Action).IsRequired();
            e.Property(a => a.Entity).IsRequired();
            e.Property(a => a.OccurredAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        base.OnModelCreating(modelBuilder);
    }
}