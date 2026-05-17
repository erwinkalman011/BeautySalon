namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Data;
using BeautySalon.Api.Models;
using BeautySalon.Api.Services;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

public class Mutation
{
     public async Task<string> Register([Service] IAuthService authService, string email, string password)
    {
        return await authService.RegisterAsync(email, password);
    }

    public async Task<string?> Login([Service] IAuthService authService, string email, string password)
    {
        return await authService.LoginAsync(email, password);
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<Category> AddCategoryAsync(string name, [Service] AppDbContext context)
    {
        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<Service> AddServiceAsync(string name, decimal price, int categoryId, [Service] AppDbContext context)
    {
        var service = new Service { Name = name, Price = price, CategoryId = categoryId };
        context.Services.Add(service);
        await context.SaveChangesAsync();
        return service;
    }
    
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<Employee> AddEmployeeAsync(string name, [Service] AppDbContext context)
    {
    var employee = new Employee { Name = name };
    context.Employees.Add(employee);
    await context.SaveChangesAsync();
    return employee;
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<string> AssignServiceToEmployeeAsync(
    int employeeId, 
    int serviceId, 
    [Service] AppDbContext context)
    {
    var employee = await context.Employees
        .Include(e => e.Services)
        .FirstOrDefaultAsync(e => e.Id == employeeId);

    var service = await context.Services
        .FirstOrDefaultAsync(s => s.Id == serviceId);

    if (employee == null) return "Eroare: Angajatul nu a fost găsit.";
    if (service == null) return "Eroare: Serviciul nu a fost găsit.";

    if (employee.Services.Any(s => s.Id == serviceId))
    {
        return $"Angajatul {employee.Name} are deja asignat serviciul {service.Name}.";
    }

    employee.Services.Add(service);
    await context.SaveChangesAsync();

    return $"Succes! Serviciul '{service.Name}' a fost asignat lui {employee.Name}.";
    }
}