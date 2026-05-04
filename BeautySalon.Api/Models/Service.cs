namespace BeautySalon.Api.Models;
public class Service 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // Relația cu Categoria
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Relația Many-to-Many cu Angajații
    public List<Employee> Employees { get; set; } = new();
}