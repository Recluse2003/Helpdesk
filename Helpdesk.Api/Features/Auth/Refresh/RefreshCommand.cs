using Helpdesk.Api.Domain.Common;
using MediatR;

namespace Helpdesk.Api.Features.Auth.Refresh
{
    public record RefreshCommand(string RefreshToken) : IRequest<Result<RefreshResponseDto>>;
}
