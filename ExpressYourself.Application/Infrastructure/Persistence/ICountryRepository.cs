using ExpressYourself.Domain.Entities;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface ICountryRepository
    {
        Task<Country?> GetByTwoLetterCodeAsync(string twoLetterCode, CancellationToken cancellationToken);
        void Add(Country country);
    }
}
