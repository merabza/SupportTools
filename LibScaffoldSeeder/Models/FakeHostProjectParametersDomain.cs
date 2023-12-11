using DbTools;
// ReSharper disable ConvertToPrimaryConstructor

namespace LibScaffoldSeeder.Models;

public sealed class FakeHostProjectParametersDomain
{
    public FakeHostProjectParametersDomain(EDataProvider dataProvider, string connectionStringSeed)
    {
        ConnectionStringSeed = connectionStringSeed;
        DataProvider = dataProvider;
    }

    public EDataProvider DataProvider { get; set; }
    public string ConnectionStringSeed { get; set; }
}