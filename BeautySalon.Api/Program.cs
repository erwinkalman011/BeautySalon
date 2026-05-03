using System.Text;
using System.Security.Claims;
using BeautySalon.Api.Data;
using BeautySalon.Api.Models;
using BeautySalon.Api.Services;
using BeautySalon.Api.GraphQL; // Aceasta linie este importanta pentru Query
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Baza de date
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=BeautySalon.db"));

// 2. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 3. Serviciile tale
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. GraphQL - Aici am adaugat ce ai nevoie
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<AuthMutation>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 5. Mapare GraphQL
app.MapGraphQL();

app.Run();