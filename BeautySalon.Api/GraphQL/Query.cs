namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Models;
using BeautySalon.Api.Data;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Security.Claims;

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
    string date,
    [Service] AppDbContext context)
{
    var availableSlots = new List<string>();

    if (!DateTime.TryParse(date, out DateTime parsedDate))
    {
        throw new GraphQLException("Formatul datei este invalid. Folosiți 'YYYY-MM-DD'.");
    }

    if (parsedDate.Date < DateTime.Today)
    {
        throw new GraphQLException("Nu puteți verifica disponibilitatea pentru o dată din trecut.");
    }
    var service = await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
    if (service == null) return availableSlots;

    var dayOfWeek = parsedDate.DayOfWeek;
    var schedule = await context.WorkSchedules.FirstOrDefaultAsync(w => 
        w.EmployeeId == employeeId && w.DayOfWeek == dayOfWeek && w.IsWorking);

    if (schedule == null) return availableSlots; 

    var existingAppointments = await context.Appointments
    .Where(a => a.EmployeeId == employeeId && 
                a.AppointmentTime.Date == parsedDate.Date && 
                a.Status == "Confirmed")
    .ToListAsync();

    TimeSpan currentSlot = schedule.StartTime;
    TimeSpan closingTime = schedule.EndTime;

    while (currentSlot + TimeSpan.FromMinutes(service.DurationInMinutes) < closingTime)
    {
        DateTime proposedStart = parsedDate.Date.Add(currentSlot);
        DateTime proposedEnd = proposedStart.AddMinutes(service.DurationInMinutes);

        bool hasOverlap = existingAppointments.Any(a => 
        proposedStart < a.EndTime && 
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
[Authorize(Roles = new[] { "Admin" })]
public async Task<List<Appointment>> GetAppointmentsAsync([Service] AppDbContext context)
{
    return await context.Appointments
        .Include(a => a.Service)
        .Include(a => a.Employee)
        .OrderByDescending(a => a.AppointmentTime)
        .ToListAsync();
}

[Authorize(Roles = new[] { "Client"})]
public async Task<List<Appointment>> GetMyAppointmentsAsync(
    ClaimsPrincipal claimsPrincipal,
    [Service] AppDbContext context)
{
    var currentUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    return await context.Appointments
        .Include(a => a.Service)
        .Include(a => a.Employee)
        .Where(a => a.UserId == currentUserId)
        .OrderByDescending(a => a.AppointmentTime)
        .ToListAsync();
}
[Authorize(Roles = new[] { "Client", "Admin"})]
public async Task<List<Service>> GetServicesWithEmployeesAsync(
        [Service] AppDbContext context)
    {
        return await context.Services
            .Include(s => s.Employees)
            .ToListAsync();
    }

[Authorize(Roles = new[] { "Admin", "Angajat" })]
    public async Task<List<Appointment>> GetEmployeeAllAppointmentsAsync(
        int employeeId,
        [Service] AppDbContext context)
    {
        return await context.Appointments
            .Include(a => a.Service)
            .Include(a => a.User) 
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AppointmentTime) 
            .ToListAsync();
    }

    [Authorize(Roles = new[] { "Admin", "Angajat" })]
    public async Task<List<Appointment>> GetEmployeeAppointmentsByDayAsync(
        int employeeId,
        string date, 
        [Service] AppDbContext context)
    {
        if (!DateTime.TryParse(date, out DateTime parsedDate))
        {
            throw new GraphQLException("Formatul datei este invalid. Folosiți 'YYYY-MM-DD'.");
        }

        return await context.Appointments
            .Include(a => a.Service)
            .Include(a => a.User)
            .Where(a => a.EmployeeId == employeeId && 
                        a.AppointmentTime.Date == parsedDate.Date &&
                        a.Status == "Confirmed")
            .OrderBy(a => a.AppointmentTime) 
            .ToListAsync();
    }
}   