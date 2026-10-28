using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Infrastructure.Persistence.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string Username { get; set; } = string.Empty;
    }
}
