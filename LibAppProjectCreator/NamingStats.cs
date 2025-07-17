namespace LibAppProjectCreator;

public static class NamingStats
{
    public const string CsProjectExtension = ".csproj";
    public const string JsonExtension = ".json";

    public static string CreateProjectSeederCodeProjectName(string scaffoldSeederProjectName)
    {
        return $"Create{scaffoldSeederProjectName}DbSeederCode";
    }

    public static string GetJsonFromScaffoldDbProjectName(string scaffoldSeederProjectName)
    {
        return $"GetJsonFromScaffold{scaffoldSeederProjectName}Db";
    }

    public static string SeedDbProjectName(string scaffoldSeederProjectName)
    {
        return $"Seed{scaffoldSeederProjectName}Db";
    }

//

    public static string DataSeedingClassLibProjectName(string scaffoldSeederProjectName)
    {
        return $"{scaffoldSeederProjectName}DbDataSeeding";
    }

    public static string ScaffoldSeederFolderName(string scaffoldSeederProjectName)
    {
        return $"{scaffoldSeederProjectName}ScaffoldSeeder";
    }

    public static string DbMigrationProjectName(string scaffoldSeederProjectName)
    {
        return $"{scaffoldSeederProjectName}DbMigration";
    }

    public static string ScaffoldSeedSecFolderName(string scaffoldSeederProjectName)
    {
        return $"{scaffoldSeederProjectName}ScaffoldSeeder.sec";
    }
}