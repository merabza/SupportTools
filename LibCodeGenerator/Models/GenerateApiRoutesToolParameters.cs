using System;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibCodeGenerator.Models;

public sealed class GenerateApiRoutesToolParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private GenerateApiRoutesToolParameters()
    {
    }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static GenerateApiRoutesToolParameters? Create(SupportToolsParameters supportToolsParameters, string projectName)
    {
        try
        {
            var externalScaffoldSeedToolParameters = new GenerateApiRoutesToolParameters();
            return externalScaffoldSeedToolParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}