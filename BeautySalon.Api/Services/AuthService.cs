using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using BeautySalon.Api.Models;
using Microsoft.Extensions.Configuration;

namespace BeautySalon.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<string> RegisterAsync(string email, string password)
    {
        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new Exception("Eroare: Formatul adresei de email este invalid. Folosește un format de tip nume@domeniu.ro");
        }

        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return string.Join(", ", result.Errors.Select(e => e.Description));
        }

        string[] roleNames = { "Admin", "Angajat", "Client" };
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var adminEmails = new List<string> { "singeorzanelena50@gmail.com", "erwinkalman1@gmail.com"};

        if (adminEmails.Contains(email))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else if (email.EndsWith("@salon.ro")) 
        {
            await _userManager.AddToRoleAsync(user, "Angajat");
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Client");
        }

        return "Success";
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            throw new Exception("Eroare: Nu există niciun cont înregistrat cu acest email.");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            throw new Exception("Eroare: Parolă incorectă. Încearcă din nou.");
        }

        return await GenerateJwtToken(user);
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}