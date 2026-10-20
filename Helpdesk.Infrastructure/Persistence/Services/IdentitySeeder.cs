using Helpdesk.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Handles seeding of roles and admin account into the database on startup. 
    /// </summary>
    public static class IdentitySeeder
    {
        /// <summary>
        /// Seeds required roles (Customer, Agent, Admin).
        /// Persist them to database if they do not exist yet. 
        /// </summary>
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Customer", "SupportAgent", "Administrator" };

            foreach (var role in roles) 
            { 
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminAccountAsync(UserManager<ApplicationUser> userManager)
        {
            var adminUsername = "admin";
            var adminEmail = "admin@gmail.com";

            // Check if admin already exists
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Create user and assign Admin role
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Administrator");
            }
        }
    }
}
