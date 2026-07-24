using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;

namespace ExpressYourself.Application.Features.IpRefresh.Commands
{
    public sealed record RefreshStoredIpsCommand : IRequest<RefreshStoredIpsResult>;
}