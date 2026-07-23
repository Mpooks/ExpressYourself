using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ExpressYourself.Infrastructure.Persistence.Repositories;

public sealed class IpAddressRepository : IIpAddressRepository
{
    private readonly ExpressYourselfDbContext _context;

    public IpAddressRepository(ExpressYourselfDbContext context)
    {
        _context = context;
    }

    public async Task<IpAddress?> GetByAddressAsync(string address, CancellationToken cancellationToken)
    {
        return await _context.IpAddresses
            .FirstOrDefaultAsync(ipAddress => ipAddress.Address == address, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAddressesByCountryCodeAsync(string twoLetterCode, CancellationToken cancellationToken)
    {
        return await _context.IpAddresses
            .AsNoTracking()
            .Where(ipAddress => ipAddress.CountryTwoLetterCode == twoLetterCode)
            .Select(ipAddress => ipAddress.Address)
            .ToListAsync(cancellationToken);
    }

    public void Add(IpAddress ipAddress)
    {
        _context.IpAddresses.Add(ipAddress);
    }

    public async Task<IReadOnlyList<IpAddress>> GetBatchAsync(string? afterAddress, int batchSize, CancellationToken cancellationToken)
    {
        IQueryable<IpAddress> query = _context.IpAddresses
            .AsNoTracking()
            .OrderBy(ipAddress => ipAddress.Address);

        if (!string.IsNullOrEmpty(afterAddress))
        {
            query = query.Where(ipAddress => string.Compare(ipAddress.Address, afterAddress) > 0);
        }

        return await query
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}