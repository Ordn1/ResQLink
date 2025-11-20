using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services.Users;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default)
    {
        // Ensure database exists
        await _db.Database.EnsureCreatedAsync(ct);

        // Ensure Admin role
        var adminRole = await _db.Set<UserRole>().FirstOrDefaultAsync(r => r.RoleName == "Admin", ct);
        if (adminRole is null)
        {
            adminRole = new UserRole { RoleName = "Admin" };
            _db.Set<UserRole>().Add(adminRole);
            await _db.SaveChangesAsync(ct);
        }

        // Ensure admin user
        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (adminUser is null)
        {
            // NOTE: Plain text password due to current model. Replace with hash/salt when model updated.
            adminUser = new User
            {
                Username = "admin",
                Password = "ChangeMe123!",
                Email = "admin@example.com",
                RoleId = adminRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null) return null;

        // Plain text comparison (current schema). Replace when hashing implemented.
        return user.Password == password ? user : null;
    }
}