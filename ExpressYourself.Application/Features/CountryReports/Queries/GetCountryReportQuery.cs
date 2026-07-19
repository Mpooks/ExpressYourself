using ExpressYourself.Application.Features.CountryReports.Contracts;
using MediatR;

namespace ExpressYourself.Application.Features.CountryReports.Queries
{
    public record GetCountryReportQuery() : IRequest<IReadOnlyList<CountryReportDto>>;
}