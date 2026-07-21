namespace ExpressYourself.Application.Caching
{
    public interface IIpInformationCache
    {
        Task<IpInformationCacheEntry?> GetAsync(string address, CancellationToken cancellationToken);

        Task SetAsync(string address, IpInformationCacheEntry entry, CancellationToken cancellationToken);

        Task RemoveAsync(string address, CancellationToken cancellationToken);
    }
}
