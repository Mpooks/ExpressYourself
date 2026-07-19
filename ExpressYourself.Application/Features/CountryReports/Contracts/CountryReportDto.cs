namespace ExpressYourself.Application.Features.CountryReports.Contracts
{
    public record CountryReportDto(string TwoLetterCountryCode, string ThreeLetterCountryCode, string CountryName, int IpAddressCount);
}
