namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Data;
using BeautySalon.Api.Models;
using BeautySalon.Api.Services;
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
    public async Task<Category> AddCategoryAsync(string name, [Service] AppDbContext context)
    {
        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task<Service> AddServiceAsync(string name, decimal price, int categoryId, [Service] AppDbContext context)
    {
        var service = new Service { Name = name, Price = price, CategoryId = categoryId };
        context.Services.Add(service);
        await context.SaveChangesAsync();
        return service;
    }
}