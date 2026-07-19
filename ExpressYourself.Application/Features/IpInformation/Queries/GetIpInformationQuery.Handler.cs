using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Strategies;
using MediatR;

namespace ExpressYourself.Application.Features.IpInformation.Queries
{
    public class GetIpInformationQueryHandler : IRequestHandler<GetIpInformationQuery, IpInformationDto>
    {
        private readonly IIpInformationProvider _ipInformationProvider;
        public GetIpInformationQueryHandler(IIpInformationProvider ipInformationProvider)
        {
            _ipInformationProvider = ipInformationProvider;
        }

        public Task<IpInformationDto> Handle(GetIpInformationQuery req, CancellationToken cancellationToken)
        {
            return _ipInformationProvider.GetIpInformationAsync(req.IpAddress, cancellationToken);
        }
    }
}
