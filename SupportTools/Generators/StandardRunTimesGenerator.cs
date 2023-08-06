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
        parameters.RunTimes.TryAdd("win10-x64", "Windows 10 x64");
    }
}