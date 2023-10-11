using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Generators;

public static class StandardEnvironmentsGenerator
{
    public static void Generate(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        //თუ არ არსებობს დაემატოს ჭკვიანი სქემები: DailyStandard, Reduce, Hourly
        parameters.Environments.TryAdd("Prod", "Production");
        parameters.Environments.TryAdd("Stage", "PreProduction");
        parameters.Environments.TryAdd("Test", "Test");
        parameters.Environments.TryAdd("Dev", "Development");
    }
}