using FluentValidation;
using Helpdesk.Api.Infrastructure.Authentication;
using Helpdesk.Api.Infrastructure.Configuration;
using Helpdesk.Api.Infrastructure.Middleware; 
using Helpdesk.Api.Infrastructure.Persistence; 
using Helpdesk.Api.Infrastructure.Persistence.Models; 
using Helpdesk.Api.Infrastructure.Persistence.Services;
using Helpdesk.Api.Infrastructure.Services;
using Helpdesk.Api.Shared.Behaviors;
using Helpdesk.Api.Shared.Extensions;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Get the current assembly 
var apiAssembly = typeof(Program).Assembly;

// Core framework configurations
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

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
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HelpdeskDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

var apiSettings = builder.Configuration
    .GetSection("ApiSettings")
    .Get<ApiSettings>()
    ?? throw new InvalidOperationException(
        "ApiSettings configuration is missing.");

if (string.IsNullOrWhiteSpace(apiSettings.Secret) ||
    string.IsNullOrWhiteSpace(apiSettings.Issuer) ||
    string.IsNullOrWhiteSpace(apiSettings.Audience))
{
    throw new InvalidOperationException(
        "ApiSettings must contain Secret, Issuer and Audience.");
}

byte[] key;

try
{
    key = Convert.FromBase64String(apiSettings.Secret);
}
catch (FormatException ex)
{
    throw new InvalidOperationException("ApiSettings:Secret must be a valid Base64 string.", ex);
}

if (key.Length < 32)
    throw new InvalidOperationException("The JWT signing key must contain at least 32 bytes.");

// Authentication & Authorization
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = apiSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = apiSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(0),

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("Email"))
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.From) &&
        !string.IsNullOrWhiteSpace(settings.SmtpServer) &&
        settings.Port > 0 &&
        !string.IsNullOrWhiteSpace(settings.Username) &&
        !string.IsNullOrWhiteSpace(settings.Password),
        "Email configuration is incomplete.")
    .ValidateOnStart();

builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// HTTP Request Pipeline Configuration
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
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

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Seed roles and admin user
    await IdentitySeeder.SeedRolesAsync(roleManager);
    await IdentitySeeder.SeedAdminAccountAsync(userManager);
}

app.Run();