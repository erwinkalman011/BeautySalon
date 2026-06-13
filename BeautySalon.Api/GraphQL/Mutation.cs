namespace BeautySalon.Api.GraphQL;

using BeautySalon.Api.Data;
using BeautySalon.Api.Models;
using BeautySalon.Api.Services;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Security.Claims;

public class Mutation
{
    // ==========================================
    // 1. ZONA PUBLICĂ (Fără restricții)
    // ==========================================

    public async Task<string> Register([Service] IAuthService authService, string email, string password)
    {
        return await authService.RegisterAsync(email, password);
    }

    public async Task<string?> Login([Service] IAuthService authService, string email, string password)
    {
        return await authService.LoginAsync(email, password);
    }

    // ==========================================
    // 2. ZONA DE MANAGEMENT (Exclusiv Admin)
    // ==========================================

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
public async Task<Employee> UpdateEmployeeAsync(
    int id, 
    string newName, 
    [Service] AppDbContext context)
{
    var employee = await context.Employees.FindAsync(id);
    if (employee == null) throw new GraphQLException("Angajatul nu a fost găsit.");
    if (string.IsNullOrWhiteSpace(newName)) throw new GraphQLException("Numele nu poate fi gol.");

    employee.Name = newName;
    await context.SaveChangesAsync();
    return employee;
}

[Authorize(Roles = new[] { "Admin" })]
public async Task<string> DeleteEmployeeAsync(int id, [Service] AppDbContext context)
{
    // Luăm angajatul simplu, fără .Include
    var employee = await context.Employees.FindAsync(id);
    if (employee == null) throw new GraphQLException("Angajatul nu a fost găsit.");

    // Căutăm direct în tabela centrală de programări dacă ID-ul fetei este implicat în viitor
    bool hasFutureAppointments = await context.Appointments.AnyAsync(a => 
        a.EmployeeId == id && 
        a.Status == "Confirmed" && 
        a.AppointmentTime >= DateTime.Now);

    if (hasFutureAppointments)
    {
        throw new GraphQLException($"Nu puteți șterge angajatul '{employee.Name}' deoarece are programări active în viitor.");
    }

    context.Employees.Remove(employee);
    await context.SaveChangesAsync();
    return $"Angajatul '{employee.Name}' a fost eliminat cu succes.";
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


    [Authorize(Roles = new[] { "Admin" })]
    public async Task<Service> UpdateServiceDurationAsync(
        int serviceId,
        int durationInMinutes,
        [Service] AppDbContext context)
    {
        var service = await context.Services.FindAsync(serviceId) ?? throw new GraphQLException("Serviciul nu a fost găsit.");
        
        if (durationInMinutes <= 0) throw new GraphQLException("Durata trebuie să fie mai mare de 0 minute.");

        service.DurationInMinutes = durationInMinutes;
        await context.SaveChangesAsync();

        return service;
    }
    [Authorize(Roles = new[] { "Admin" })]
public async Task<Service> UpdateServicePriceAsync(
    int serviceId, 
    decimal newPrice, 
    [Service] AppDbContext context)
{
    var service = await context.Services.FindAsync(serviceId);
    if (service == null) 
    {
        throw new GraphQLException("Serviciul nu a fost găsit.");
    }
    
    if (newPrice <= 0) 
    {
        throw new GraphQLException("Prețul trebuie să fie mai mare decât 0.");
    }

    // Datorită denormalizării din tabela Appointment, această modificare 
    // NU va altera prețul programărilor deja efectuate în trecut!
    service.Price = newPrice;
    await context.SaveChangesAsync();
    
    return service;
}

[Authorize(Roles = new[] { "Admin" })]
public async Task<string> DeleteServiceAsync(int serviceId, [Service] AppDbContext context)
{
    // Încărcăm serviciul simplu, fără .Include
    var service = await context.Services.FindAsync(serviceId);
    if (service == null) 
    {
        throw new GraphQLException("Serviciul nu a fost găsit.");
    }

    // Căutăm direct în tabela centrală de programări folosind serviceId
    bool hasFutureAppointments = await context.Appointments.AnyAsync(a => 
        a.ServiceId == serviceId && 
        a.Status == "Confirmed" && 
        a.AppointmentTime >= DateTime.Now);

    if (hasFutureAppointments)
    {
        throw new GraphQLException($"Nu puteți șterge serviciul '{service.Name}' deoarece există programări viitoare active asociate lui.");
    }

    context.Services.Remove(service);
    await context.SaveChangesAsync();
    
    return $"Serviciul '{service.Name}' a fost șters din catalog.";
}

    // ==========================================
    // 3. ZONA DE PROGRAMĂRI (Client, Angajat, Admin)
    // ==========================================

    [Authorize(Roles = new[] { "Client", "Admin" })]
    public async Task<Appointment> CreateAppointmentAsync(
        int employeeId,
        int serviceId,
        string appointmentTime,
        ClaimsPrincipal claimsPrincipal,
        [Service] AppDbContext context)
    {
        if (!DateTime.TryParse(appointmentTime, out DateTime parsedStartTime))
        {
            throw new GraphQLException("Formatul datei/orei este invalid. Folosiți 'YYYY-MM-DDTHH:mm:ss'.");
        }
        var userId = (claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value) ?? throw new GraphQLException("Unauthorized.");
        
        if (parsedStartTime < DateTime.Now)
        {
            throw new GraphQLException("Nu puteți efectua o programare pentru o dată sau o oră din trecut.");
        }

        var service = await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId) ?? throw new GraphQLException("Serviciul nu există.");

        var employee = await context.Employees
        .Include(e => e.Services)
        .FirstOrDefaultAsync(e => e.Id == employeeId) ?? throw new GraphQLException("Angajatul selectat nu există.");

        bool employeeProvidesService = employee.Services.Any(s => s.Id == serviceId);
        if (!employeeProvidesService)
        {
            throw new GraphQLException($"Angajatul {employee.Name} nu oferă serviciul '{service.Name}'.");
        }
        
        int duration = service.DurationInMinutes;
        DateTime proposedStart = parsedStartTime;
        DateTime proposedEnd = parsedStartTime.AddMinutes(duration);

        var dayOfAppointment = parsedStartTime.DayOfWeek;
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
            AppointmentTime = parsedStartTime,
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
        var userId = (claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value) ?? throw new GraphQLException("Neautorizat.");
        
        var isClient = claimsPrincipal.IsInRole("Client");

        var appointment = await context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId) ?? throw new GraphQLException("Programarea nu a fost găsită.");
        
        if (isClient && appointment.UserId != userId)
        {
            throw new GraphQLException("Nu aveți permisiunea de a anula această programare.");
        }

        if (isClient && appointment.AppointmentTime.AddHours(-24) < DateTime.Now)
        {
            throw new GraphQLException("Politica salonului: Programările nu mai pot fi anulate cu mai puțin de 24 de ore înainte.");
        }

        appointment.Status = "Canceled";
        await context.SaveChangesAsync();

        return "Programarea a fost anulată cu succes.";
    }

}