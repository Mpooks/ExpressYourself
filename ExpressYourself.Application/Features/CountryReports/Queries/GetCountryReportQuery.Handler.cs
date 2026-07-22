using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using MediatR;

namespace ExpressYourself.Application.Features.CountryReports.Queries
{
    public class GetCountryReportQueryHandler : IRequestHandler<GetCountryReportQuery, IReadOnlyList<CountryReportDto>>
    {
        private readonly ICountryReportRepository _countryReportRepository;

        public GetCountryReportQueryHandler(ICountryReportRepository countryReportRepository)
        {
            _countryReportRepository = countryReportRepository;
        }

        public async Task<IReadOnlyList<CountryReportDto>> Handle(GetCountryReportQuery req, CancellationToken cancellationToken)
        {
            IReadOnlyList<string>? normalizedCodes = NormalizeCodes(req.Codes);
            return await _countryReportRepository.GetAllAsync(normalizedCodes, cancellationToken);
        }

        private static IReadOnlyList<string>? NormalizeCodes(IReadOnlyList<string>? codes)
        {
            if (codes is null || codes.Count == 0)
            {
                return null;
            }


            string[] normalizedCodes = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToArray();

            if (normalizedCodes.Length == 0)
            {
                return null;
            }

            foreach (var code in normalizedCodes)
            {
                if (code.Length != 2 || !code.All(char.IsAsciiLetter))
                    throw new InvalidCountryCodeException($"'{code}' is not a valid two-letter country code.");
            }

            return normalizedCodes;
        }
    }
}
