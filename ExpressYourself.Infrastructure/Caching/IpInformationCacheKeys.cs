using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Infrastructure.Caching
{
    internal static class IpInformationCacheKeys
    {
        private const string AddressPrefix = "ip-info:";

        public static string ForAddress(string address)
        {
            string normalizedAddress = IpAddressValidator.Normalize(address);

            return $"{AddressPrefix}{normalizedAddress}";
        }
    }
}
