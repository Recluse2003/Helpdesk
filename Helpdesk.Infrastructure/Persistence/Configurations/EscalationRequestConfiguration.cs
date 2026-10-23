using Helpdesk.Domain.Entities;
using Helpdesk.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations
{
    public class EscalationRequestConfiguration : IEntityTypeConfiguration<EscalationRequest>
    {
        public void Configure(EntityTypeBuilder<EscalationRequest> builder)
        {
            builder.HasKey(er => er.Id);

            builder.Property(er => er.Reason)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(er => er.ResponseReason)
                .HasMaxLength(5000);

            builder.Property(er => er.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(er => er.CreatedAt)
                .IsRequired();

            builder.Property(er => er.RespondedAt);

            builder.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(er => er.TicketId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(er => er.RequestedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(er => er.ProposedRecipientUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(er => er.RespondedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(er => er.TicketId);
        }
    }
}
