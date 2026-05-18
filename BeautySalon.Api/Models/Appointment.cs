using System;
using Microsoft.AspNetCore.Identity;
namespace BeautySalon.Api.Models;

public class Appointment
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateTime AppointmentTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationInMinutes { get; set; }

    public string Status { get; set; } = "Confirmed";
}