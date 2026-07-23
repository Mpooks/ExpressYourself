namespace ExpressYourself.Application.Features.CountryReports.Contracts
{
    public record CountryReportDto(string CountryName, int AddressesCount, DateTimeOffset LastAddressUpdated);
}
