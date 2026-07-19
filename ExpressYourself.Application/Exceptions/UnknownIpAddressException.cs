namespace ExpressYourself.Application.Errors
{
    public class UnknownIpAddressException : Exception
    {
        public string IpAddress { get; }
        public UnknownIpAddressException(string ipAddress) : base($"No country information was found for {ipAddress}") 
        {
            IpAddress = ipAddress;
        }

    }
}
