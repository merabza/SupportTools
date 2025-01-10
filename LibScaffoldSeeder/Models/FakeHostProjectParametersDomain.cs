using LibDatabaseParameters;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibScaffoldSeeder.Models;

public sealed class FakeHostProjectParametersDomain
{
    public FakeHostProjectParametersDomain(EDatabaseProvider dataProvider, string connectionStringSeed)
    {
        ConnectionStringSeed = connectionStringSeed;
        DataProvider = dataProvider;
    }

    public EDatabaseProvider DataProvider { get; set; }
    public string ConnectionStringSeed { get; set; }
}