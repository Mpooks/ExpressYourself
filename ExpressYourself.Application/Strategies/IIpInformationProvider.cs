using ExpressYourself.Application.Features.IpInformation.Contracts;

namespace ExpressYourself.Application.Strategies
{
    public interface IIpInformationProvider
    {
        Task<IpInformationDto> GetIpInformationAssignedAsync(string address, CancellationToken cancellationToken);
    }
}
