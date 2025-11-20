using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ResQLink.Services.Users;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default)
    {
        await _db.Database.EnsureCreatedAsync(ct);

        var adminRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Admin", ct);
        if (adminRole is null)
        {
            adminRole = new UserRole { RoleName = "Admin", Description = "System administrator" };
            _db.UserRoles.Add(adminRole);
            await _db.SaveChangesAsync(ct);
        }

        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (adminUser is null)
        {
            var pwd = "ChangeMe123!";
            var hash = HashPassword(pwd);
            adminUser = new User
            {
                Username = "admin",
                PasswordHash = hash,
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
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);
        if (user is null) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    private static string HashPassword(string password)
    {
        // Simple SHA256 for placeholder; replace with PBKDF2/Argon2 later
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}