using LibAppProjectCreator;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.SeedData;

// ReSharper disable once UnusedType.Global
public class SeedDataFactoryStrategy : ExternalScaffoldSeedToolCommandFactoryStrategy,
    IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SeedDataFactoryStrategy(ILogger<SeedDataFactoryStrategy> logger)
        : base(logger)
    {
    }

    public string ToolCommandName => nameof(EProjectTools.JsonFromProjectDbProjectGetter);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project != null)
        {
            return CreateToolCommand(parametersManager, projectName, NamingStats.SeedDbProjectName,
                project.SeedProjectFilePath, project.SeedProjectParametersFilePath);
        }

        StShared.WriteErrorLine($"Project with name {projectName} not found", true);
        return null;

    }
}
