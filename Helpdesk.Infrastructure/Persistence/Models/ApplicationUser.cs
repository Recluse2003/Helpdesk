using Microsoft.AspNetCore.Identity;

namespace Helpdesk.Infrastructure.Persistence.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
