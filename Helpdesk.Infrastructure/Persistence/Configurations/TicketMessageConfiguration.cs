using Helpdesk.Domain.Entities;
using Helpdesk.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations
{
    public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
    {
        public void Configure(EntityTypeBuilder<TicketMessage> builder)
        {
            builder.HasKey(tm => tm.Id);

            builder.Property(tm => tm.Content)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(tm => tm.IsPublic)
                .IsRequired();

            builder.Property(tm => tm.CreatedAt)
                .IsRequired();

            builder.HasOne<Ticket>()
                .WithMany(t => t.Messages)
                .HasForeignKey(tm => tm.TicketId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(tm => tm.AuthorUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(tm => tm.TicketId);
        }
    }
}
