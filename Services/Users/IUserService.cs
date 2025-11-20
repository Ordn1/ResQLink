using ResQLink.Data.Entities;

namespace ResQLink.Services.Users;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default); // Added for startup seeding
}