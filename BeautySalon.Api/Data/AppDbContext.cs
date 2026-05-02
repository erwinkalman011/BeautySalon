using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeautySalon.Api.Models;

namespace BeautySalon.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Aici vor veni mai târziu DbSet-urile pentru restul tabelelor (Servicii, Programări etc.)
}