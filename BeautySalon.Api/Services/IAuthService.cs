using BeautySalon.Api.Models;

namespace BeautySalon.Api.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(string email, string password);
    Task<string?> LoginAsync(string email, string password);
}