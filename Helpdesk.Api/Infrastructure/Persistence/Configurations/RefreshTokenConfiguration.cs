using Helpdesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Api.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configures the Entity Framework Core database mapping for the <see cref="RefreshToken"/> entity.
    /// </summary>
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Id)
                .ValueGeneratedNever();

            builder.Property(rt => rt.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique();

            builder.HasIndex(rt => rt.UserId);
        }
    }
}
