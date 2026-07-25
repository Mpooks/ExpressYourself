using ExpressYourself.Application.Interfaces;

namespace ExpressYourself.Tests.Integration
{
    public sealed class FakeIp2cClient : IIp2cClient
    {
        private Ip2cLookupResult _ip2cLookupResult;
        private Exception? _exceptionCase;
        private int _callCount;
        private TimeSpan _delay;

        public int CallCounter { get {  return _callCount; }  }

        public FakeIp2cClient()
        {
            _ip2cLookupResult = new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null);
            _exceptionCase = null;
            _callCount = 0;
            _delay = TimeSpan.Zero;
        }
        public void Returns(Ip2cLookupResult result)
        {
            _ip2cLookupResult = result;
            _exceptionCase = null;
            _callCount = 0;
            _delay = TimeSpan.Zero;
        }

        public void Throws(Exception exception)
        {
            _exceptionCase = exception;
            _callCount = 0;
            _delay = TimeSpan.Zero;
        }

        public void ReturnAfterDelay(Ip2cLookupResult result, TimeSpan delay)
        {
            _ip2cLookupResult = result;
            _exceptionCase = null;
            _callCount = 0;
            _delay = delay;
        }

        public async Task<Ip2cLookupResult> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_exceptionCase is not null)
            {
                throw _exceptionCase;
            }

            return _ip2cLookupResult;
        }
    }
}
