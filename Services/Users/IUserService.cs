using ResQLink.Data.Entities;

namespace ResQLink.Services.Users;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task LogLogoutAsync(int userId, string username, string email, string? role); // 🔥 FIXED: Added email parameter
    Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default);
    Task<(User? user, string? error)> RegisterUserAsync(string username, string password, string email, int roleId, int registeredByUserId, CancellationToken ct = default);
    Task<List<UserRole>> GetAllRolesAsync(CancellationToken ct = default);
    Task<List<User>> GetAllUsersAsync(CancellationToken ct = default);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateUserAsync(int userId, string? email, int? roleId, bool? isActive, CancellationToken ct = default);
    Task<(bool success, string? error)> ResetPasswordAsync(int userId, string newPassword, CancellationToken ct = default);
    Task<(bool success, string message)> ResetPasswordByUsernameAsync(string username, string newPassword, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteUserAsync(int userId, CancellationToken ct = default);
    Task<bool> ValidateAdminAccessAsync(int userId, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}