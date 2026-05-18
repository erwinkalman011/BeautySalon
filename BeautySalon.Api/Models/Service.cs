namespace BeautySalon.Api.Models;
public class Service 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public int DurationInMinutes { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public List<Employee> Employees { get; set; } = new();
}