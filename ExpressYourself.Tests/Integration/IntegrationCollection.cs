namespace ExpressYourself.Tests.Integration
{
    [CollectionDefinition(Name)]
    public sealed class IntegrationCollection : ICollectionFixture<ApiFactory>
    {
        public const string Name = "Integration";
    }
}
