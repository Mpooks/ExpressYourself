using ExpressYourself.Domain.Entities;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface IIpAddressRepository
    {
        Task<IpAddress?> GetByAddressAsync(string address, CancellationToken cancellationToken);
        void Add(IpAddress ipAddress);
    }
}
