using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ExpressYourself.Infrastructure.Persistence.Repositories;

internal sealed class CountryRepository : ICountryRepository
{
    private readonly ExpressYourselfDbContext _context;

    public CountryRepository(ExpressYourselfDbContext context)
    {
        _context = context;
    }

    public async Task<Country?> GetByTwoLetterCodeAsync(string twoLetterCode, CancellationToken cancellationToken)
    {
        return await _context.Countries
            .FirstOrDefaultAsync(c => c.TwoLetterCode == twoLetterCode, cancellationToken);
    }

    public void Add(Country country)
    {
        _context.Countries.Add(country);
    }
}
