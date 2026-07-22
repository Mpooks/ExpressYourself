namespace ExpressYourself.Application.Interfaces
{
    public interface IIp2cClient
    {
        Task<Ip2cLookupResult> GetIpInformationAsync(string address, CancellationToken cancellationToken);
    }
}
