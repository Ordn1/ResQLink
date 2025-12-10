using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ResQLink.Services.Users;

public class UserService(AppDbContext db, AuditService? auditService = null) : IUserService
{
    private readonly AppDbContext _db = db;
    private readonly AuditService? _auditService = auditService;

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

        // Seed Operation Officer role
        var opOfficerRole = await _db.UserRoles.FirstOrDefaultAsync(r => r.RoleName == "Operation Officer", ct);
        if (opOfficerRole is null)
        {
            opOfficerRole = new UserRole { RoleName = "Operation Officer", Description = "Manages disaster operations and response" };
            _db.UserRoles.Add(opOfficerRole);
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
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);
        
        if (user is null)
        {
            // Log failed login attempt
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "LOGIN",
                    entityType: "User",
                    entityId: null,
                    userId: null,
                    userType: null,
                    userName: username,
                    description: $"Failed login attempt for username '{username}': User not found or inactive",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Invalid credentials"
                );
            }
            return null;
        }

        // Check if account is locked
        if (IsAccountLocked(user))
        {
            var remainingMinutes = Math.Ceiling((user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes);
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "LOGIN",
                    entityType: "User",
                    entityId: user.UserId,
                    userId: user.UserId,
                    userType: user.Role?.RoleName,
                    userName: $"{user.Username} ({user.Email})",
                    description: $"Failed login attempt for locked account '{user.Username}'. Account locked for {remainingMinutes} more minutes.",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: $"Account is locked. Try again in {remainingMinutes} minutes or reset your password."
                );
            }
            return null; // Return null to indicate locked account
        }

        var isValid = VerifyPassword(password, user.PasswordHash);
        
        if (!isValid)
        {
            // Record failed login attempt
            await RecordFailedLoginAttemptAsync(user, ct);
            
            // Log failed attempt
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "LOGIN",
                    entityType: "User",
                    entityId: user.UserId,
                    userId: user.UserId,
                    userType: user.Role?.RoleName,
                    userName: $"{user.Username} ({user.Email})",
                    description: $"Failed login attempt for user '{user.Username}': Invalid password. Attempts: {user.FailedLoginAttempts}",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Invalid password"
                );
            }
            
            return null;
        }
        
        // Successful login - reset failed attempts
        await ResetLoginAttemptsAsync(user, ct);
        
        // Log login attempt (success or failure)
        if (_auditService != null)
        {
            await _auditService.LogAsync(
                action: "LOGIN",
                entityType: "User",
                entityId: user.UserId,
                userId: user.UserId,
                userType: user.Role?.RoleName,
                userName: $"{user.Username} ({user.Email})",
                description: $"User {user.Username} logged in successfully",
                severity: "Info",
                isSuccessful: true
            );
        }
        
        return user;
    }

    // Check if account is currently locked
    private static bool IsAccountLocked(User user)
    {
        if (user.LockoutEnd == null)
            return false;
            
        if (user.LockoutEnd > DateTime.UtcNow)
            return true;
            
        // Lockout period has expired, return false but don't reset here
        return false;
    }
    
    // Record a failed login attempt and lock account if threshold reached
    private async Task RecordFailedLoginAttemptAsync(User user, CancellationToken ct = default)
    {
        user.FailedLoginAttempts++;
        
        // Lock account for 5 minutes after 3 failed attempts
        if (user.FailedLoginAttempts >= 3)
        {
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(5);
            user.RequiresPasswordReset = true; // Force password reset
        }
        
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
    
    // Reset failed login attempts on successful login
    private async Task ResetLoginAttemptsAsync(User user, CancellationToken ct = default)
    {
        if (user.FailedLoginAttempts > 0 || user.LockoutEnd != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            // Note: RequiresPasswordReset is NOT reset here - only by actual password reset
            
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
        }
    }

    // Reset password for a user (unlocks account and clears failed attempts)
    public async Task<(bool success, string message)> ResetPasswordByUsernameAsync(string username, string newPassword, CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            
            if (user == null)
            {
                return (false, "User not found");
            }
            
            // Update password
            user.PasswordHash = HashPassword(newPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.RequiresPasswordReset = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
            
            // Log password reset
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PASSWORD_RESET",
                    entityType: "User",
                    entityId: user.UserId,
                    userId: user.UserId,
                    userType: user.Role?.RoleName,
                    userName: $"{user.Username} ({user.Email})",
                    description: $"Password reset for user '{user.Username}'. Account unlocked.",
                    severity: "Info",
                    isSuccessful: true
                );
            }
            
            return (true, "Password reset successfully. You can now login with your new password.");
        }
        catch (Exception ex)
        {
            return (false, $"Error resetting password: {ex.Message}");
        }
    }

    // Log logout
    public async Task LogLogoutAsync(int userId, string username, string email, string? role)
    {
        if (_auditService == null) return;
        
        await _auditService.LogAsync(
            action: "LOGOUT",
            entityType: "User",
            entityId: userId,
            userId: userId,
            userType: role,
            userName: $"{username} ({email})",
            description: $"User {username} logged out",
            severity: "Info",
            isSuccessful: true
        );
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

            // Validate password strength
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

            // Log user registration
            if (_auditService != null)
            {
                var registeredBy = await _db.Users
                    .Include(u => u.Role)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == registeredByUserId, ct);

                await _auditService.LogAsync(
                    action: "USER_REGISTER",
                    entityType: "User",
                    entityId: newUser.UserId,
                    userId: registeredByUserId,
                    userType: registeredBy?.Role?.RoleName,
                    userName: registeredBy?.Username,
                    newValues: new
                    {
                        newUser.UserId,
                        newUser.Username,
                        newUser.Email,
                        Role = newUser.Role?.RoleName
                    },
                    description: $"New user '{newUser.Username}' registered by {registeredBy?.Username} with role {newUser.Role?.RoleName}",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (newUser, null);
        }
        catch (DbUpdateException ex)
        {
            // Log registration error
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "USER_REGISTER",
                    entityType: "User",
                    entityId: null,
                    userId: registeredByUserId,
                    userType: null,
                    userName: username,
                    description: $"Failed to register user '{username}'",
                    severity: "Error",
                    isSuccessful: false,
                    errorMessage: ex.InnerException?.Message ?? ex.Message
                );
            }
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
        int? updatedByUserId = null,
        CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null)
                return (false, "User not found");

            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "User data is invalid");

            // Capture old values for audit
            var oldEmail = user.Email;
            var oldRoleId = user.RoleId;
            var oldIsActive = user.IsActive;

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.Contains('@'))
                    return (false, "Invalid email address");

                var emailExists = await _db.Users
                    .AnyAsync(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.UserId != userId, ct);
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
            
            _db.Users.Attach(user);
            _db.Entry(user).State = EntityState.Modified;
            
            await _db.SaveChangesAsync(ct);

            // Log user update
            if (_auditService != null && (email != null || roleId.HasValue || isActive.HasValue))
            {
                var oldRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == oldRoleId, ct);
                var newRole = await _db.UserRoles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == user.RoleId, ct);
                
                var updatedBy = updatedByUserId.HasValue 
                    ? await _db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == updatedByUserId.Value, ct)
                    : null;

                await _auditService.LogAsync(
                    action: "USER_UPDATE",
                    entityType: "User",
                    entityId: userId,
                    userId: updatedByUserId,
                    userType: updatedBy?.Role?.RoleName ?? "Admin",
                    userName: updatedBy != null ? $"{updatedBy.Username} ({updatedBy.Email})" : "System",
                    oldValues: new
                    {
                        Email = oldEmail,
                        Role = oldRole?.RoleName,
                        IsActive = oldIsActive
                    },
                    newValues: new
                    {
                        user.Email,
                        Role = newRole?.RoleName,
                        user.IsActive
                    },
                    description: $"User '{user.Username}' updated by {updatedBy?.Username ?? "System"}",
                    severity: "Info",
                    isSuccessful: true
                );
            }

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
        int? resetByUserId = null,
        CancellationToken ct = default)
    {
        try
        {
            // Load tracked user entity (avoid AsNoTracking/Attach to prevent duplicate tracked Role)
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);
            
            if (user is null)
                return (false, "User not found");

            // Ensure role is available for audit logging without duplicating tracked instances
            await _db.Entry(user).Reference(u => u.Role).LoadAsync(ct);

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 12)
                return (false, "Password must be at least 12 characters");

            if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsDigit))
                return (false, "Password must contain at least one uppercase letter and one number");

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            // No Attach/Modified needed since entity is tracked
            await _db.SaveChangesAsync(ct);

            // Log password reset
            if (_auditService != null)
            {
                var resetBy = resetByUserId.HasValue 
                    ? await _db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == resetByUserId.Value, ct)
                    : null;

                await _auditService.LogAsync(
                    action: "PASSWORD_RESET",
                    entityType: "User",
                    entityId: userId,
                    userId: resetByUserId,
                    userType: resetBy?.Role?.RoleName ?? "Admin",
                    userName: resetBy != null ? $"{resetBy.Username} ({resetBy.Email})" : "System",
                    description: $"Password reset for user '{user.Username}' by {resetBy?.Username ?? "System"}",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

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

    public async Task<(bool success, string? error)> DeleteUserAsync(int userId, int? deletedByUserId = null, CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user is null)
                return (false, "User not found");

            var userWithRole = await _db.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            if (userWithRole?.Role?.RoleName == "Admin" && userWithRole.IsActive)
            {
                var activeAdminCount = await _db.Users
                    .Include(u => u.Role)
                    .CountAsync(u => u.Role != null && u.Role.RoleName == "Admin" && u.IsActive, ct);
                
                if (activeAdminCount <= 1)
                    return (false, "Cannot delete the last active admin user");
            }

            // Capture user details before deletion
            var userDetails = new
            {
                user.UserId,
                user.Username,
                user.Email,
                Role = userWithRole?.Role?.RoleName,
                user.IsActive
            };

            var userProfile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (userProfile is not null)
            {
                _db.UserProfiles.Attach(userProfile);
                _db.UserProfiles.Remove(userProfile);
            }

            _db.Users.Attach(user);
            _db.Users.Remove(user);
            
            await _db.SaveChangesAsync(ct);

            // Log user deletion
            if (_auditService != null)
            {
                var deletedBy = deletedByUserId.HasValue 
                    ? await _db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == deletedByUserId.Value, ct)
                    : null;

                await _auditService.LogAsync(
                    action: "USER_DELETE",
                    entityType: "User",
                    entityId: userId,
                    userId: deletedByUserId,
                    userType: deletedBy?.Role?.RoleName ?? "Admin",
                    userName: deletedBy != null ? $"{deletedBy.Username} ({deletedBy.Email})" : "System",
                    oldValues: userDetails,
                    description: $"User '{user.Username}' deleted by {deletedBy?.Username ?? "System"}",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

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

    // NEW: Track user profile updates
    public async Task<(bool success, string? error)> UpdateUserProfileAsync(
        int userId,
        string firstName,
        string lastName,
        string contactNumber,
        string address,
        int? updatedByUserId = null,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await _db.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
                return (false, "User profile not found");

            var user = await _db.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            // Capture old values
            var oldValues = new
            {
                profile.FirstName,
                profile.LastName,
                profile.ContactNumber,
                profile.Address
            };

            profile.FirstName = firstName;
            profile.LastName = lastName;
            profile.ContactNumber = contactNumber;
            profile.Address = address;

            _db.UserProfiles.Attach(profile);
            _db.Entry(profile).State = EntityState.Modified;
            await _db.SaveChangesAsync(ct);

            // Log profile update
            if (_auditService != null)
            {
                var updatedBy = updatedByUserId.HasValue 
                    ? await _db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == updatedByUserId.Value, ct)
                    : null;

                await _auditService.LogAsync(
                    action: "USER_PROFILE_UPDATE",
                    entityType: "UserProfile",
                    entityId: profile.UserProfileId,
                    userId: updatedByUserId ?? userId,
                    userType: updatedBy?.Role?.RoleName ?? user?.Role?.RoleName,
                    userName: updatedBy != null ? $"{updatedBy.Username} ({updatedBy.Email})" : user?.Username,
                    oldValues: oldValues,
                    newValues: new
                    {
                        firstName,
                        lastName,
                        contactNumber,
                        address
                    },
                    description: $"Profile updated for user '{user?.Username}'",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Profile update failed: {ex.Message}");
        }
    }

    // NEW: Track user session activity
    public async Task LogUserActivityAsync(int userId, string action, string description, CancellationToken ct = default)
    {
        if (_auditService == null) return;

        var user = await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user != null)
        {
            await _auditService.LogAsync(
                action: action,
                entityType: "UserActivity",
                entityId: userId,
                userId: userId,
                userType: user.Role?.RoleName,
                userName: $"{user.Username} ({user.Email})",
                description: description,
                severity: "Info",
                isSuccessful: true
            );
        }
    }

    // NEW: Get user transaction history
    public async Task<List<AuditLog>> GetUserTransactionHistoryAsync(int userId, DateTime? _startDate = null, DateTime? _endDate = null, CancellationToken _ct = default)
    {
        if (_auditService == null) return [];

        return await _auditService.GetUserActivityAsync(userId, limit: 1000);
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
        return await _db.Users.AnyAsync(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase), ct);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    // Explicit interface implementations for missing IUserService members

    public async Task<(bool success, string? error)> UpdateUserAsync(
        int userId,
        string? email,
        int? roleId,
        bool? isActive,
        CancellationToken ct = default)
    {
        // Call the main UpdateUserAsync with default updatedByUserId
        return await UpdateUserAsync(userId, email, roleId, isActive, null, ct);
    }

    public async Task<(bool success, string? error)> ResetPasswordAsync(
        int userId,
        string newPassword,
        CancellationToken ct = default)
    {
        // Call the main ResetPasswordAsync with default resetByUserId
        return await ResetPasswordAsync(userId, newPassword, null, ct);
    }

    public async Task<(bool success, string? error)> DeleteUserAsync(
        int userId,
        CancellationToken ct = default)
    {
        // Call the main DeleteUserAsync with default deletedByUserId
        return await DeleteUserAsync(userId, null, ct);
    }
}