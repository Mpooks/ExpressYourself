namespace ExpressYourself.Gateway.Exceptions
{
    public sealed class Ip2cResponseFormatException : Exception
    {
        public Ip2cResponseFormatException(string message)
            : base(message)
        {
        }
    }
}
