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
            return await _countryReportRepository.GetAllAsync(cancellationToken);
        }
    }
}
