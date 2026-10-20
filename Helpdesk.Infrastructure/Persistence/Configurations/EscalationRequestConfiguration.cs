using Helpdesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations
{
    public class EscalationRequestConfiguration : IEntityTypeConfiguration<EscalationRequest>
    {
        public void Configure(EntityTypeBuilder<EscalationRequest> builder)
        {

        }
    }
}
