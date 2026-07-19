using ExpressYourself.Application.Features.CountryReports.Contracts;

namespace ExpressYourself.Application.Infrastructure.Persistence
{
    public interface ICountryReportRepository
    {
        Task<IReadOnlyList<CountryReportDto>> GetAllAsync(IReadOnlyCollection<string> twoLetterCodes ,CancellationToken cancellationToken = default);
    }
}
