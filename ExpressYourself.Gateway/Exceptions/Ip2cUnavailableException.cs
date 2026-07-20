namespace ExpressYourself.Gateway.Exceptions
{
    public sealed class Ip2cUnavailableException : Exception
    {
        public Ip2cUnavailableException(string message)
            : base(message)
        {
        }
    }
}
