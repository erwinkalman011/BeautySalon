namespace BeautySalon.Api.GraphQL;
using BeautySalon.Api.Data;
using BeautySalon.Api.Models;
using BeautySalon.Api.Services;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Security.Claims;

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

    [Authorize(Roles = new[] { "Client", "Admin" })]
    public async Task<Appointment> CreateAppointmentAsync(
        int employeeId,
        int serviceId,
        DateTime appointmentTime,
        ClaimsPrincipal claimsPrincipal,
        [Service] AppDbContext context)
    {
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) throw new GraphQLException("Unauthorized.");

        var service = await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
        if (service == null) throw new GraphQLException("Serviciul nu există.");

        int duration = service.DurationInMinutes;
        DateTime proposedStart = appointmentTime;
        DateTime proposedEnd = appointmentTime.AddMinutes(duration);

        var dayOfAppointment = appointmentTime.DayOfWeek;
        var schedule = await context.WorkSchedules.FirstOrDefaultAsync(w =>
            w.EmployeeId == employeeId &&
            w.DayOfWeek == dayOfAppointment &&
            w.IsWorking);

        if (schedule == null)
        {
            throw new GraphQLException("Angajatul nu lucrează în ziua propusă. Vă rugăm să alegeți o altă zi.");
        }

        TimeSpan proposedStartTimeSpan = proposedStart.TimeOfDay;
        TimeSpan proposedEndTimeSpan = proposedEnd.TimeOfDay;

        if (proposedStartTimeSpan < schedule.StartTime || proposedEndTimeSpan > schedule.EndTime)
        {
            throw new GraphQLException($"În afara programului de lucru. Tura angajatului este: {schedule.StartTime} - {schedule.EndTime}");
        }

        var isOverlapping = await context.Appointments
            .AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.Status == "Confirmed" &&
                proposedStart < a.EndTime &&
                proposedEnd > a.AppointmentTime
            );

        if (isOverlapping)
        {
            throw new GraphQLException("Intervalul propus se suprapune cu o programare existentă. Vă rugăm să alegeți un alt interval.");
        }

        var appointment = new Appointment
        {
            UserId = userId,
            EmployeeId = employeeId,
            ServiceId = serviceId,
            AppointmentTime = appointmentTime,
            DurationInMinutes = duration,
            EndTime = proposedEnd,
            Status = "Confirmed"
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        return appointment;
    }
    [Authorize (Roles = new[] { "Client", "Admin" })]
public async Task<string> CancelAppointmentAsync(
    int appointmentId,
    ClaimsPrincipal claimsPrincipal,
    [Service] AppDbContext context)
{
    var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) throw new GraphQLException("Neautorizat.");

    var appointment = await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
    if (appointment == null) throw new GraphQLException("Programarea nu a fost găsită.");

    if (appointment.UserId != userId)
    {
        throw new GraphQLException("Nu aveți permisiunea de a anula această programare.");
    }

    if (appointment.AppointmentTime.AddHours(-24) < DateTime.Now)
    {
        throw new GraphQLException("Politica salonului: Programările nu mai pot fi anulate cu mai puțin de 24 de ore înainte.");
    }

    appointment.Status = "Canceled";
    await context.SaveChangesAsync();

    return "Programarea a fost anulată cu succes.";
}

public async Task<WorkSchedule> AddWorkScheduleAsync(
    int employeeId,
    DayOfWeek dayOfWeek, 
    string startTime,    
    string endTime,      
    [Service] AppDbContext context)
{
    var employee = await context.Employees.FindAsync(employeeId);
    if (employee == null) throw new GraphQLException("Angajatul nu există.");

    if (!TimeSpan.TryParse(startTime, out TimeSpan start) || 
        !TimeSpan.TryParse(endTime, out TimeSpan end))
    {
        throw new GraphQLException("Formatul orelor este invalid. Folosiți 'HH:mm:ss'.");
    }

    var schedule = new WorkSchedule
    {
        EmployeeId = employeeId,
        DayOfWeek = dayOfWeek,
        StartTime = start,
        EndTime = end,
        IsWorking = true
    };

    context.WorkSchedules.Add(schedule);
    await context.SaveChangesAsync();
    
    return schedule;
}
}