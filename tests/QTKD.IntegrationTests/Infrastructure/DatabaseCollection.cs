namespace QTKD.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<QtkdApiFactory>
{
    public const string Name = "Database";
}
