using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Generators;

public static class StandardRunTimesGenerator
{
    public static void Generate(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        //თუ არ არსებობს დაემატოს ჭკვიანი სქემები: DailyStandard, Reduce, Hourly
        parameters.RunTimes.TryAdd("linux-x64",
            "Most desktop distributions like CentOS, Debian, Fedora, Ubuntu, and derivatives");
        parameters.RunTimes.TryAdd("win-x64", "Windows x64");
    }
}