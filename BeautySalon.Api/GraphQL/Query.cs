namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Models;
using BeautySalon.Api.Data;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<string>> GetAvailableSlotsAsync(
    int employeeId,
    int serviceId,
    DateTime date,
    [Service] AppDbContext context)
{
    var availableSlots = new List<string>();

    var service = await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
    if (service == null) return availableSlots;

    var dayOfWeek = date.DayOfWeek;
    var schedule = await context.WorkSchedules.FirstOrDefaultAsync(w => 
        w.EmployeeId == employeeId && w.DayOfWeek == dayOfWeek && w.IsWorking);

    if (schedule == null) return availableSlots; 

    var existingAppointments = await context.Appointments
        .Include(a => a.Service)
        .Where(a => a.EmployeeId == employeeId && 
                    a.AppointmentTime.Date == date.Date && 
                    a.Status == "Confirmed")
        .ToListAsync();

    TimeSpan currentSlot = schedule.StartTime;
    TimeSpan closingTime = schedule.EndTime;

    while (currentSlot + TimeSpan.FromMinutes(service.DurationInMinutes) <= closingTime)
    {
        DateTime proposedStart = date.Date.Add(currentSlot);
        DateTime proposedEnd = proposedStart.AddMinutes(service.DurationInMinutes);

        bool hasOverlap = existingAppointments.Any(a => 
            proposedStart < a.AppointmentTime.AddMinutes(a.Service.DurationInMinutes) && 
            proposedEnd > a.AppointmentTime
        );

        if (!hasOverlap)
        {
            availableSlots.Add(currentSlot.ToString(@"hh\:mm"));
        }

        currentSlot = currentSlot.Add(TimeSpan.FromMinutes(30));
    }

    return availableSlots;
}    
}   