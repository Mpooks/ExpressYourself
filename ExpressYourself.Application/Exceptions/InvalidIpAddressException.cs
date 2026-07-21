using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressYourself.Application.Exceptions
{
    
    public class InvalidIpAddressException : Exception
    {
        public string? InvalidIpAddress { get; }
        public InvalidIpAddressException(string message, string? invalidIp) : base(message)
        {
            InvalidIpAddress = invalidIp;
        }
    }
}
