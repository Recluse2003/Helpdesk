using FluentValidation;
using Helpdesk.Api.Infrastructure.Middleware; 
using Helpdesk.Api.Infrastructure.Persistence; 
using Helpdesk.Api.Infrastructure.Persistence.Models; 
using Helpdesk.Api.Infrastructure.Persistence.Services;
using Helpdesk.Api.Shared.Behaviors;
using Helpdesk.Api.Shared.Extensions; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Get the current assembly 
var apiAssembly = typeof(Program).Assembly;

// Core framework configurations
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Consolidated Application registrations 
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(apiAssembly);
    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(apiAssembly);

// Persistence setup
builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Helpdesk"))
);

// Identity core configuration
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HelpdeskDbContext>()
    .AddSignInManager();

// Authentication & Authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// HTTP Request Pipeline Configuration
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map all feature endpoints
app.MapFeatures();
app.MapControllers();

// Database Migrations and Seeding Setup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<HelpdeskDbContext>();

    // Apply pending migrations automatically
    await dbContext.Database.MigrateAsync();

    // Fixed a small bug: passing IdentityRole<Guid> to match AddRoles structure
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Seed roles and admin user
    await IdentitySeeder.SeedRolesAsync(roleManager);
    await IdentitySeeder.SeedAdminAccountAsync(userManager);
}

app.Run();