using ExpressYourself.Application.Features.IpInformation.Contracts;
using MediatR;

namespace ExpressYourself.Application.Features.IpInformation.Queries
{
    public record GetIpInformationQuery(string IpAddress) : IRequest<IpInformationDto>;
}
