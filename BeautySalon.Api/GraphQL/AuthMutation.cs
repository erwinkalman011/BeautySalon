using BeautySalon.Api.Services;
using HotChocolate;

namespace BeautySalon.Api.GraphQL; // Trebuie să fie exact așa

public class AuthMutation
{
    public async Task<string> Register([Service] IAuthService authService, string email, string password)
    {
        return await authService.RegisterAsync(email, password);
    }

    public async Task<string?> Login([Service] IAuthService authService, string email, string password)
    {
        return await authService.LoginAsync(email, password);
    }
}