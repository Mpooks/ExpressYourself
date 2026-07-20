using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpressYourself.Infrastructure.Persistence.Repositories;

internal sealed class CountryRepository : ICountryRepository
{
    private readonly ExpressYourselfDbContext _context;

    public CountryRepository(ExpressYourselfDbContext context)
    {
        _context = context;
    }

    public async Task<Country?> GetByTwoLetterCodeAsync(string twoLetterCode)
    {

        return await _context.Countries
        .FirstOrDefaultAsync(c => c.TwoLetterCode == twoLetterCode);
    }

    public void Add(Country country)
    {
        _context.Countries.Add(country);
    }
}
