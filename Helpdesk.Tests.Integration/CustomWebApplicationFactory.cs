
using Helpdesk.Api.Infrastructure.Persistence;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Helpdesk.Api.Infrastructure.Persistence.Services;
using Helpdesk.Api.Shared.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Helpdesk.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeEmailService FakeEmailService { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://example.com",
                        ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Helpdesk_IntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
                    });
            });

            builder.ConfigureServices(services =>
            {
                // Replace the application's DbContext registration.
                services.RemoveAll<DbContextOptions<HelpdeskDbContext>>();
                services.RemoveAll<HelpdeskDbContext>();

                services.AddDbContext<HelpdeskDbContext>(options => options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Helpdesk_IntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

                // Replace the real email service.
                services.RemoveAll<IEmailService>();

                services.AddSingleton(FakeEmailService);
                services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailService>());
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            dbContext.Database.Migrate();

            return host;
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            await SeedIdentityAsync(Services);

            FakeEmailService.Clear();
        }

        private static async Task SeedIdentityAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await IdentitySeeder.SeedRolesAsync(roleManager);
            await IdentitySeeder.SeedAdminAccountAsync(userManager);
        }
    }
};