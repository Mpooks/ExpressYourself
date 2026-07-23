using ExpressYourself.Domain.Entities;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface IIpAddressRepository
    {
        Task<IpAddress?> GetByAddressAsync(string address, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> GetAddressesByCountryCodeAsync(string twoLetterCode, CancellationToken cancellationToken);

        Task<IReadOnlyList<IpAddress>> GetBatchAsync(string? afterAddress, int batchSize, CancellationToken cancellationToken);

        void Add(IpAddress ipAddress);
    }
}