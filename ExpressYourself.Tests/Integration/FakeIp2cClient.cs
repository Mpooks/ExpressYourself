using ExpressYourself.Application.Interfaces;

namespace ExpressYourself.Tests.Integration
{
    public sealed class FakeIp2cClient : IIp2cClient
    {
        private Ip2cLookupResult _ip2cLookupResult;
        private Exception? _exceptionCase;

        public FakeIp2cClient()
        {
            _ip2cLookupResult = new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null);
            _exceptionCase = null;
        }
        public void Returns(Ip2cLookupResult result)
        {
            _ip2cLookupResult = result;
            _exceptionCase = null;
        }

        public void Throws(Exception exception)
        {
            _exceptionCase = exception;
        }

        public Task<Ip2cLookupResult> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            if (_exceptionCase is not null)
            {
                throw _exceptionCase;
            }

            return Task.FromResult(_ip2cLookupResult);
        }
    }
}
