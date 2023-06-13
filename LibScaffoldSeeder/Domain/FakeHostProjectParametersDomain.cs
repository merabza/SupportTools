using DbTools;

namespace LibScaffoldSeeder.Domain;

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