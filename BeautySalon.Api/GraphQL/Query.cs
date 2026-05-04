namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Models;
using BeautySalon.Api.Data;
using HotChocolate;
using HotChocolate.Data;      
public class Query
{
    public string Salut() => "Serverul GraphQL functioneaza!";

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Service> GetServices([Service] AppDbContext context) 
        => context.Services;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Employee> GetEmployees([Service] AppDbContext context) 
        => context.Employees;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Category> GetCategories([Service] AppDbContext context) 
        => context.Categories;
}   