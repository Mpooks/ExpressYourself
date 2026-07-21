

using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Exceptions;
using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Application.Features.IpInformation.Queries
{
    public class GetIpInformationQueryValidator : IValidator<GetIpInformationQuery>
    {
       

        public void Validate(GetIpInformationQuery instance)
        {
            var address = instance.IpAddress;
            try
            {
                IpAddressValidator.Normalize(address);
            } catch (DomainValidationException)
            {
                throw new InvalidIpAddressException($"Ip Address {address} is invalid.", address);
            }
        }
    }
}
