using ExpressYourself.Domain.Entities;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface ICountryRepository
    {
        Task<Country?> GetByTwoLetterCodeAsync(string twoLetterCode);
        void Add(Country country);
    }
}
