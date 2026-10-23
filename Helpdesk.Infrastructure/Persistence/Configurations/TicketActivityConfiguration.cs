using Helpdesk.Domain.Entities;
using Helpdesk.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations
{
    public class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
    {
        public void Configure(EntityTypeBuilder<TicketActivity> builder)
        {
            builder.HasKey(ta => ta.Id);

            builder.Property(ta => ta.EventType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ta => ta.IsPublic)
                .IsRequired();

            builder.Property(ta => ta.Details)
                .HasMaxLength(1000);

            builder.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(ta => ta.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ta => ta.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(er => er.TicketId);
        }
    }
}
