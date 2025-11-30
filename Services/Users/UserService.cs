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

        // Seed Admin role
        var adminRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Admin", ct);
        if (adminRole is null)
        {
            adminRole = new UserRole { RoleName = "Admin", Description = "System administrator with full access" };
            _db.UserRoles.Add(adminRole);
            await _db.SaveChangesAsync(ct);
        }

        // Seed Inventory Manager role
        var invManagerRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Inventory Manager", ct);
        if (invManagerRole is null)
        {
            invManagerRole = new UserRole { RoleName = "Inventory Manager", Description = "Manages relief goods and stocks" };
            _db.UserRoles.Add(invManagerRole);
            await _db.SaveChangesAsync(ct);
        }

        // Seed Finance Manager role
        var finManagerRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Finance Manager", ct);
        if (finManagerRole is null)
        {
            finManagerRole = new UserRole { RoleName = "Finance Manager", Description = "Manages donations and financial records" };
            _db.UserRoles.Add(finManagerRole);
            await _db.SaveChangesAsync(ct);
        }

        // Seed Volunteer role
        var volunteerRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Volunteer", ct);
        if (volunteerRole is null)
        {
            volunteerRole = new UserRole { RoleName = "Volunteer", Description = "Assists with operations and distributions" };
            _db.UserRoles.Add(volunteerRole);
            await _db.SaveChangesAsync(ct);
        }

        // Seed default admin user
        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (adminUser is null)
        {
            var pwd = "ChangeMe123!";
            var hash = HashPassword(pwd);
            adminUser = new User
            {
                Username = "admin",
                PasswordHash = hash,
                Email = "admin@resqlink.com",
                RoleId = adminRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync(ct);

            // Create UserProfile for admin
            var adminProfile = new UserProfile
            {
                UserId = adminUser.UserId,
                FirstName = "Admin",
                LastName = "User",
                ContactNumber = string.Empty,
                Address = string.Empty
            };
            _db.UserProfiles.Add(adminProfile);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);
        
        if (user is null) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public async Task<(User? user, string? error)> RegisterUserAsync(
        string username, 
        string password, 
        string email, 
        int roleId, 
        int registeredByUserId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate username
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return (null, "Username must be at least 3 characters");

            if (await UsernameExistsAsync(username, ct))
                return (null, "Username already exists");

            // Validate email
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return (null, "Invalid email address");

            if (await EmailExistsAsync(email, ct))
                return (null, "Email already registered");

            // Validate password strength - Updated to require 12 characters
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                return (null, "Password must be at least 12 characters");

            if (!password.Any(char.IsUpper) || !password.Any(char.IsDigit))
                return (null, "Password must contain at least one uppercase letter and one number");

            // Validate role exists
            var roleExists = await _db.UserRoles.AnyAsync(r => r.RoleId == roleId, ct);
            if (!roleExists)
                return (null, "Invalid role selected");

            // Hash password
            var hash = HashPassword(password);

            var newUser = new User
            {
                Username = username.Trim(),
                PasswordHash = hash,
                Email = email.Trim().ToLower(),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync(ct);

            // Create UserProfile entry
            var userProfile = new UserProfile
            {
                UserId = newUser.UserId,
                FirstName = string.Empty,
                LastName = string.Empty,
                ContactNumber = string.Empty,
                Address = string.Empty
            };
            _db.UserProfiles.Add(userProfile);
            await _db.SaveChangesAsync(ct);

            // Reload with role
            await _db.Entry(newUser).Reference(u => u.Role).LoadAsync(ct);

            return (newUser, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Registration failed: {ex.Message}");
        }
    }

    public async Task<List<UserRole>> GetAllRolesAsync(CancellationToken ct = default)
    {
        return await _db.UserRoles
            .OrderBy(r => r.RoleName)
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);
    }

    public async Task<(bool success, string? error)> UpdateUserAsync(
        int userId, 
        string? email, 
        int? roleId, 
        bool? isActive, 
        CancellationToken ct = default)
    {
        try
        {
            // Use AsNoTracking for the initial query, then attach with explicit state
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null)
                return (false, "User not found");

            // Ensure all required fields are set
            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "User data is invalid");

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.Contains('@'))
                    return (false, "Invalid email address");

                var emailExists = await _db.Users
                    .AnyAsync(u => u.Email == email.ToLower() && u.UserId != userId, ct);
                if (emailExists)
                    return (false, "Email already in use");

                user.Email = email.Trim().ToLower();
            }

            if (roleId.HasValue)
            {
                var roleExists = await _db.UserRoles.AnyAsync(r => r.RoleId == roleId.Value, ct);
                if (!roleExists)
                    return (false, "Invalid role selected");

                user.RoleId = roleId.Value;
            }

            if (isActive.HasValue)
            {
                user.IsActive = isActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            
            // Attach and mark as modified
            _db.Users.Attach(user);
            _db.Entry(user).State = EntityState.Modified;
            
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Update failed: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> ResetPasswordAsync(
        int userId, 
        string newPassword, 
        CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null)
                return (false, "User not found");

            // Validate password strength
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 12)
                return (false, "Password must be at least 12 characters");

            if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsDigit))
                return (false, "Password must contain at least one uppercase letter and one number");

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            // Attach and mark as modified
            _db.Users.Attach(user);
            _db.Entry(user).State = EntityState.Modified;
            
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Password reset failed: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null)
                return (false, "User not found");

            // Load role information to check if admin
            var userWithRole = await _db.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            // Prevent deleting the last admin user
            if (userWithRole?.Role?.RoleName == "Admin" && userWithRole.IsActive)
            {
                var activeAdminCount = await _db.Users
                    .Include(u => u.Role)
                    .CountAsync(u => u.Role != null && u.Role.RoleName == "Admin" && u.IsActive, ct);
                
                if (activeAdminCount <= 1)
                    return (false, "Cannot delete the last active admin user");
            }

            // Delete associated UserProfile first (if exists)
            var userProfile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (userProfile is not null)
            {
                _db.UserProfiles.Attach(userProfile);
                _db.UserProfiles.Remove(userProfile);
            }

            // Delete user
            _db.Users.Attach(user);
            _db.Users.Remove(user);
            
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Delete failed: {ex.Message}");
        }
    }

    public async Task<bool> ValidateAdminAccessAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive, ct);

        return user?.Role?.RoleName == "Admin";
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(u => u.Username == username, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(u => u.Email == email.ToLower(), ct);
    }

    private static string HashPassword(string password)
    {
        // Using SHA256 for now - consider upgrading to PBKDF2 or Argon2 for production
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