namespace ExpressYourself.Tests.Integration
{
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly ApiFactory ApiFactory;

        protected IntegrationTestBase(ApiFactory apiFactory)
        {
            ApiFactory = apiFactory;
        }

        public async Task InitializeAsync()   // xUnit runs this before EVERY test
        {
            await ApiFactory.ResetStateAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}