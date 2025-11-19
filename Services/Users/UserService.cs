using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ResQLink.Services.Users;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default)
    {
        // Create tables if DB exists but is empty
        await _db.Database.EnsureCreatedAsync(ct);

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Username == "Admin", ct);
        if (admin is null)
        {
            var (hash, salt) = HashPassword("Adminpassword");
            _db.Users.Add(new User
            {
                Username = "Admin",
                Role = "Admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                Email = "admin@example.com",
                CreatedUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user is null) return false;
        return VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[16];
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        var computed = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hash),
            Convert.FromBase64String(computed));
    }
}