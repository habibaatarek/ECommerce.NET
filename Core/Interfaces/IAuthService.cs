namespace Core.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(string email, string password, string role);
    Task<string?> LoginAsync(string email, string password);
    string GenerateJwtToken(Guid userId, string email, string role);
}

