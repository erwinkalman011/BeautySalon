namespace BeautySalon.Api.Models;
public class Category 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Service> Services { get; set; } = new();
}