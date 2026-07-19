using ExpressYourself.Application.Features.IpInformation.Contracts;

namespace ExpressYourself.Application.Strategies
{
    public interface IIpInformationProvider
    {
        Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken);
    }
}
