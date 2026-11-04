using Helpdesk.Api.Domain.Entities;
using Helpdesk.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Api.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configures the Entity Framework Core database mapping for the <see cref="TicketActivity"/> entity.
    /// </summary>
    public class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
    {
        public void Configure(EntityTypeBuilder<TicketActivity> builder)
        {
            builder.HasKey(ta => ta.Id);

            builder.Property(ta => ta.ActivityType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ta => ta.IsPublic)
                .IsRequired();

            builder.Property(ta => ta.Details)
                .HasMaxLength(1000);

            builder.HasOne<Ticket>()
                .WithMany(t => t.Activities)
                .HasForeignKey(ta => ta.TicketId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ta => ta.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(er => er.TicketId);
        }
    }
}
