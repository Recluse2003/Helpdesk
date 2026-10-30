using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Infrastructure.Persistence
{
    public class HelpdeskDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
        public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
        public DbSet<EscalationRequest> EscalationRequests => Set<EscalationRequest>();

        public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpdeskDbContext).Assembly);
        }
    }
}
