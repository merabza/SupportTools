using LibAppProjectCreator;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;

namespace LibScaffoldSeeder.ToolCommands.JsonFromProjectDbProjectGetter;

public class JsonFromProjectDbProjectGetterFactoryStrategy : ExternalScaffoldSeedToolCommandFactoryStrategy,
    IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public JsonFromProjectDbProjectGetterFactoryStrategy(ILogger<JsonFromProjectDbProjectGetterFactoryStrategy> logger)
        : base(logger)
    {
    }

    public string ToolCommandName => nameof(EProjectTools.JsonFromProjectDbProjectGetter);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        return CreateToolCommand(parametersManager, projectName, NamingStats.GetJsonFromScaffoldDbProjectName);
    }
}
