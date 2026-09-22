using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectToolRunnerTests : IDisposable
{
    private const string ProjectName = "MyProject";
    private const string ServerName = "dl360";
    private const string EnvironmentName = "Prod";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private ServiceProvider? _serviceProvider;

    public ProjectToolRunnerTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _serviceProvider?.Dispose();
    }

    [Fact]
    public void GetAllToolNames_ReturnsNamesOfBothToolEnums()
    {
        // Act
        string[] names = ProjectToolRunner.GetAllToolNames();

        // Assert
        Assert.Equal(Enum.GetNames<EProjectTools>().Length + Enum.GetNames<EProjectServerTools>().Length,
            names.Length);
        Assert.Contains(nameof(EProjectTools.RecreateDevDatabase), names, StringComparer.Ordinal);
        Assert.Contains(nameof(EProjectServerTools.ServiceStarter), names, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("RecreateDevDatabase", EProjectTools.RecreateDevDatabase)]
    [InlineData("recreatedevdatabase", EProjectTools.RecreateDevDatabase)]
    [InlineData("SEEDDATA", EProjectTools.SeedData)]
    public void FindProjectTool_WhenNameIsKnown_ReturnsToolIgnoringCase(string toolName, EProjectTools expected)
    {
        Assert.Equal(expected, ProjectToolRunner.FindProjectTool(toolName));
    }

    [Theory]
    [InlineData("NoSuchTool")]
    [InlineData("2")]
    [InlineData("")]
    [InlineData("ServiceStarter")]
    public void FindProjectTool_WhenNameIsNotAProjectToolName_ReturnsNull(string toolName)
    {
        Assert.Null(ProjectToolRunner.FindProjectTool(toolName));
    }

    [Theory]
    [InlineData("ServiceStarter", EProjectServerTools.ServiceStarter)]
    [InlineData("progpublisher", EProjectServerTools.ProgPublisher)]
    public void FindServerTool_WhenNameIsKnown_ReturnsToolIgnoringCase(string toolName, EProjectServerTools expected)
    {
        Assert.Equal(expected, ProjectToolRunner.FindServerTool(toolName));
    }

    [Theory]
    [InlineData("NoSuchTool")]
    [InlineData("1")]
    [InlineData("RecreateDevDatabase")]
    public void FindServerTool_WhenNameIsNotAServerToolName_ReturnsNull(string toolName)
    {
        Assert.Null(ProjectToolRunner.FindServerTool(toolName));
    }

    [Fact]
    public async Task Run_WhenProjectIsNotRegistered_ReturnsFalse()
    {
        // Act
        bool result = await ProjectToolRunner.Run(CreateServiceProvider(), _parametersManager.Object, ProjectName,
            EProjectTools.RecreateDevDatabase);

        // Assert
        Assert.False(result);
        Assert.Contains($"Project with name {ProjectName} not found", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenToolIsNotInAllowToolsList_ReturnsFalseAndListsAllowedTools()
    {
        // Arrange
        AddProject(EProjectTools.SeedData, EProjectTools.RecreateDevDatabase);

        // Act
        bool result = await ProjectToolRunner.Run(CreateServiceProvider(), _parametersManager.Object, ProjectName,
            EProjectTools.DropDevDatabase);

        // Assert
        Assert.False(result);
        Assert.Contains($"Tool {EProjectTools.DropDevDatabase} is not allowed for project {ProjectName}",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Allowed tools are: RecreateDevDatabase, SeedData", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenAllowToolsListHasUndefinedValue_DoesNotOfferItAsAllowed()
    {
        // Arrange
        AddProject(EProjectTools.SeedData, (EProjectTools)99);

        // Act
        bool result = await ProjectToolRunner.Run(CreateServiceProvider(), _parametersManager.Object, ProjectName,
            EProjectTools.DropDevDatabase);

        // Assert
        Assert.False(result);
        Assert.Contains("Allowed tools are: SeedData", ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain("99", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenNoStrategyIsRegisteredForTool_ReturnsFalse()
    {
        // Arrange
        AddProject(EProjectTools.RecreateDevDatabase);

        // Act
        bool result = await ProjectToolRunner.Run(CreateServiceProvider(), _parametersManager.Object, ProjectName,
            EProjectTools.RecreateDevDatabase);

        // Assert
        Assert.False(result);
        Assert.Contains($"No strategy found for tool {EProjectTools.RecreateDevDatabase}", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenStrategyCreatesNoToolCommand_ReturnsFalse()
    {
        // Arrange
        AddProject(EProjectTools.RecreateDevDatabase);
        ServiceProvider serviceProvider =
            CreateServiceProvider(CreateStrategy(nameof(EProjectTools.RecreateDevDatabase), null));

        // Act
        bool result = await ProjectToolRunner.Run(serviceProvider, _parametersManager.Object, ProjectName,
            EProjectTools.RecreateDevDatabase);

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Tool {EProjectTools.RecreateDevDatabase} for project {ProjectName} could not be created",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WhenToolCommandIsCreated_ReturnsItsResult(bool toolCommandResult)
    {
        // Arrange
        AddProject(EProjectTools.RecreateDevDatabase);
        Mock<IToolCommand> toolCommand = CreateToolCommand(toolCommandResult);
        ServiceProvider serviceProvider =
            CreateServiceProvider(CreateStrategy(nameof(EProjectTools.RecreateDevDatabase), toolCommand.Object));
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        bool result = await ProjectToolRunner.Run(serviceProvider, _parametersManager.Object, ProjectName,
            EProjectTools.RecreateDevDatabase, cancellationTokenSource.Token);

        // Assert
        Assert.Equal(toolCommandResult, result);
        toolCommand.Verify(x => x.Run(cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task RunOnServer_WhenProjectIsNotRegistered_ReturnsFalse()
    {
        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains($"Project with name {ProjectName} not found", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunOnServer_WhenServerIsNotFound_ReturnsFalseAndListsExistingServers()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "guria/Prod", "guria", EnvironmentName, EProjectServerTools.ServiceStarter);

        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains($"Server with name {ServerName} not found for project {ProjectName}", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Contains("Existing servers are: guria|Prod", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunOnServer_WhenSeveralServersMatchTheName_ReturnsFalse()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "first", ServerName, "Dev", EProjectServerTools.ServiceStarter);
        AddServerInfo(project, "second", ServerName, "Test", EProjectServerTools.ServiceStarter);

        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains($"More than one server found with name {ServerName} for project {ProjectName}", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunOnServer_WhenServerToolIsNotAllowed_ReturnsFalseAndListsAllowedTools()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "key", ServerName, EnvironmentName, EProjectServerTools.ProgPublisher);

        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Tool {EProjectServerTools.ServiceStarter} is not allowed for project {ProjectName} on server {ServerName}|{EnvironmentName}",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Allowed tools are: ProgPublisher", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunOnServer_WhenServerHasNoAllowToolsList_ReturnsFalse()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "key", ServerName, EnvironmentName).AllowToolsList = null;

        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains("Allowed tools are: ", ConsoleText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("serverKey")]
    [InlineData("dl360|Prod")]
    [InlineData("dl360")]
    [InlineData("DL360")]
    public async Task RunOnServer_WhenServerIsAddressedInAnySupportedWay_RunsTheToolCommand(string addressedServerName)
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "serverKey", ServerName, EnvironmentName, EProjectServerTools.ServiceStarter);
        _parameters.Servers.Add(ServerName, new ServerDataModel());
        Mock<IToolCommand> toolCommand = CreateToolCommand(true);
        ServiceProvider serviceProvider = CreateServiceProvider(
            CreateStrategy(nameof(EProjectServerTools.ServiceStarter), toolCommand.Object));

        // Act
        bool result = await ProjectToolRunner.RunOnServer(serviceProvider, _parametersManager.Object, ProjectName,
            addressedServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.True(result);
        toolCommand.Verify(x => x.Run(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunOnServer_WhenStrategyCreatesNoToolCommand_ReturnsFalse()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "serverKey", ServerName, EnvironmentName, EProjectServerTools.ServiceStarter);
        _parameters.Servers.Add(ServerName, new ServerDataModel());
        ServiceProvider serviceProvider =
            CreateServiceProvider(CreateStrategy(nameof(EProjectServerTools.ServiceStarter), null));

        // Act
        bool result = await ProjectToolRunner.RunOnServer(serviceProvider, _parametersManager.Object, ProjectName,
            ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Tool {EProjectServerTools.ServiceStarter} for project {ProjectName} on server {ServerName}|{EnvironmentName} could not be created",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunOnServer_WhenServerIsNotRegisteredInParameters_ReturnsFalse()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "serverKey", ServerName, EnvironmentName, EProjectServerTools.ServiceStarter);
        ServiceProvider serviceProvider = CreateServiceProvider(
            CreateStrategy(nameof(EProjectServerTools.ServiceStarter), CreateToolCommand(true).Object));

        // Act
        bool result = await ProjectToolRunner.RunOnServer(serviceProvider, _parametersManager.Object, ProjectName,
            ServerName, EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains($"Server with name {ServerName} not found", ConsoleText(), StringComparison.Ordinal);
    }
    [Fact]
    public async Task RunOnServer_WhenServerIsNotFound_ListsExistingServersSortedAndSeparated()
    {
        // Arrange
        ProjectModel project = AddProject();
        AddServerInfo(project, "second", "guria", EnvironmentName, EProjectServerTools.ServiceStarter);
        AddServerInfo(project, "first", "bee", EnvironmentName, EProjectServerTools.ServiceStarter);

        // Act
        bool result = await ProjectToolRunner.RunOnServer(CreateServiceProvider(), _parametersManager.Object,
            ProjectName, "nosuchserver", EProjectServerTools.ServiceStarter);

        // Assert
        Assert.False(result);
        Assert.Contains($"Existing servers are: bee|{EnvironmentName}, guria|{EnvironmentName}", ConsoleText(),
            StringComparison.Ordinal);
    }

    private string ConsoleText()

    {
        return _consoleOutput.ToString();
    }

    private ProjectModel AddProject(params EProjectTools[] allowedTools)
    {
        var project = new ProjectModel { AllowToolsList = [.. allowedTools] };
        _parameters.Projects.Add(ProjectName, project);
        return project;
    }

    private static ServerInfoModel AddServerInfo(ProjectModel project, string key, string serverName,
        string environmentName, params EProjectServerTools[] allowedTools)
    {
        var serverInfo = new ServerInfoModel
        {
            ServerName = serverName, EnvironmentName = environmentName, AllowToolsList = [.. allowedTools]
        };
        project.ServerInfos.Add(key, serverInfo);
        return serverInfo;
    }

    private ServiceProvider CreateServiceProvider(params IToolCommandFactoryStrategy[] strategies)
    {
        var serviceCollection = new ServiceCollection();
        foreach (IToolCommandFactoryStrategy strategy in strategies)
        {
            serviceCollection.AddSingleton(strategy);
        }

        _serviceProvider = serviceCollection.BuildServiceProvider();
        return _serviceProvider;
    }

    private static IToolCommandFactoryStrategy CreateStrategy(string toolCommandName, IToolCommand? toolCommand)
    {
        var strategy = new Mock<IToolCommandFactoryStrategy>();
        strategy.SetupGet(x => x.ToolCommandName).Returns(toolCommandName);
        strategy.Setup(x => x.CreateToolCommand(It.IsAny<IParametersManager>(),
            It.IsAny<IFactoryStrategyParameters>(), It.IsAny<CancellationToken>())).ReturnsAsync(toolCommand);
        return strategy.Object;
    }

    private static Mock<IToolCommand> CreateToolCommand(bool runResult)
    {
        var toolCommand = new Mock<IToolCommand>();
        toolCommand.Setup(x => x.Run(It.IsAny<CancellationToken>())).ReturnsAsync(runResult);
        return toolCommand;
    }
}
