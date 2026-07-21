using ExpressYourself.Domain.Entities;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface IIpAddressRepository
    {
        Task<IpAddress?> GetByAddressAsync(string address, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> GetAddressesByCountryCodeAsync(string twoLetterCode, CancellationToken cancellationToken);

        void Add(IpAddress ipAddress);
    }
}