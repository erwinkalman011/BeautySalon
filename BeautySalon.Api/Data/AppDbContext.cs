using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeautySalon.Api.Models;

namespace BeautySalon.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    // Mai târziu vei adăuga și public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Foarte important: apelează base.OnModelCreating când folosești Identity!
        base.OnModelCreating(modelBuilder);

        // Configurăm relația Many-to-Many între Employee și Service
        modelBuilder.Entity<Employee>()
            .HasMany(e => e.Services)
            .WithMany(s => s.Employees)
            .UsingEntity(j => j.ToTable("EmployeeServices"));
    }
}