using LibCodeGenerator.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibCodeGenerator.ToolCommands.GenerateApiRoutes;

public class GenerateApiRoutesToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<GenerateApiRoutesToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateApiRoutesToolCommandFactoryStrategy(ILogger<GenerateApiRoutesToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.GenerateApiRoutes);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var generateApiRoutesParameters = GenerateApiRoutesToolParameters.Create(supportToolsParameters, projectName);
        if (generateApiRoutesParameters is not null)
        {
            return new GenerateApiRoutesToolCommand(_logger, parametersManager, generateApiRoutesParameters);
        }

        StShared.WriteErrorLine("generateApiRoutesParameters is null", true);
        return null;
    }
}
