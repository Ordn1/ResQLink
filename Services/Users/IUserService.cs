namespace ResQLink.Services.Users;

public interface IUserService
{
    Task EnsureCreatedAndSeedAdminAsync(CancellationToken ct = default);

    Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}