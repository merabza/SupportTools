using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliTools.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.SupportToolsParametersEdit;

public sealed class SupportToolsParametersEditorTests
{
    //the order is the order of the fields in the parameters editor menu
    private static readonly string[] ExpectedPropertyNames =
    [
        nameof(SupportToolsParameters.MachineName),
        nameof(SupportToolsParameters.CurrentMachineServerName),
        nameof(SupportToolsParameters.PathMappings),
        nameof(SupportToolsParameters.HideRemoteOnlyProjects),
        nameof(SupportToolsParameters.DotnetTools),
        nameof(SupportToolsParameters.SupportToolsServerWebApiClientName),
        nameof(SupportToolsParameters.AutoSyncWithSupportToolsServer),
        nameof(SupportToolsParameters.SupportToolsServerSyncTimeoutSeconds),
        nameof(SupportToolsParameters.LocalPackageManagerWebApiClientName),
        nameof(SupportToolsParameters.LogFolder),
        nameof(SupportToolsParameters.LogGitWork),
        nameof(SupportToolsParameters.GitExecutablePath),
        nameof(SupportToolsParameters.WorkFolder),
        nameof(SupportToolsParameters.FolderForGitignoreFiles),
        nameof(SupportToolsParameters.FolderForEditorConfigFiles),
        nameof(SupportToolsParameters.RecentCommandsFileName),
        nameof(SupportToolsParameters.RecentCommandsCount),
        nameof(SupportToolsParameters.TempFolder),
        nameof(SupportToolsParameters.CodeGenerateTestFolder),
        nameof(SupportToolsParameters.SecurityFolder),
        nameof(SupportToolsParameters.PublisherWorkFolder),
        nameof(SupportToolsParameters.ServiceDescriptionSignature),
        nameof(SupportToolsParameters.UploadTempExtension),
        nameof(SupportToolsParameters.ProgramArchiveDateMask),
        nameof(SupportToolsParameters.ProgramArchiveExtension),
        nameof(SupportToolsParameters.ParametersFileDateMask),
        nameof(SupportToolsParameters.ParametersFileExtension),
        nameof(SupportToolsParameters.MediatRLicenseKey),
        nameof(SupportToolsParameters.FileStorageNameForExchange),
        nameof(SupportToolsParameters.SmartSchemaNameForLocal),
        nameof(SupportToolsParameters.SmartSchemaNameForExchange),
        nameof(SupportToolsParameters.LocalInstallerSettings),
        nameof(SupportToolsParameters.DatabasesBackupFilesExchangeParameters),
        nameof(SupportToolsParameters.ApiClients),
        nameof(SupportToolsParameters.Gits),
        nameof(SupportToolsParameters.ReactAppTemplates),
        nameof(SupportToolsParameters.NpmPackages),
        nameof(SupportToolsParameters.FileStorages),
        nameof(SupportToolsParameters.DatabaseServerConnections),
        nameof(SupportToolsParameters.SmartSchemas),
        nameof(SupportToolsParameters.Archivers),
        nameof(SupportToolsParameters.Projects),
        nameof(SupportToolsParameters.Servers),
        nameof(SupportToolsParameters.RunTimes),
        nameof(SupportToolsParameters.GitIgnorePatterns),
        nameof(SupportToolsParameters.EditorConfigPatterns),
        nameof(SupportToolsParameters.Environments)
    ];

    private readonly SupportToolsParameters _parameters = new() { EditorConfigPatterns = { "CSharp" } };
    private readonly Mock<IParametersManager> _parametersManager = new();

    public SupportToolsParametersEditorTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCreated_NamesEditorAndKeepsEditedParameters()
    {
        // Act
        SupportToolsParametersEditor sut = CreateSut();

        // Assert
        Assert.Equal("Support Tools Parameters Editor", sut.Name);
        Assert.Same(_parameters, sut.Parameters);
    }

    [Fact]
    public void Constructor_WhenCreated_RegistersParameterFieldEditorsInMenuOrder()
    {
        // Act
        List<FieldEditor> fieldEditors = GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(ExpectedPropertyNames, fieldEditors.Select(x => x.PropertyName));
    }

    [Fact]
    public void EditorConfigPatternsEditor_WhenSubMenuRequested_OpensEditorConfigPatternsList()
    {
        // Arrange
        FieldEditor editorConfigPatternsEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.EditorConfigPatterns));

        // Act
        CliMenuSet? subMenu = editorConfigPatternsEditor.GetSubMenu(_parameters);

        // Assert
        Assert.Equal("EditorConfig Patterns", subMenu?.Caption);
        Assert.Contains("CSharp", CliMenuTestAccess.GetMenuItems(subMenu!).Select(x => x.MenuItemName));
    }

    //MachineName is optional: an empty value means the name of this computer, and the editor shows it
    [Fact]
    public void MachineNameEditor_WhenMachineNameIsNotSet_ShowsNameOfThisComputer()
    {
        // Arrange
        FieldEditor machineNameEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.MachineName));

        // Act
        string status = machineNameEditor.GetValueStatus(_parameters);

        // Assert
        Assert.IsType<MachineNameFieldEditor>(machineNameEditor);
        Assert.Equal(Environment.MachineName, status);
    }

    //D2: auto-sync is opt-in, and the timeout of a new parameters file is the default one
    [Fact]
    public void AutoSyncEditors_WhenParametersAreNew_ShowAutoSyncOffAndDefaultTimeout()
    {
        // Arrange
        List<FieldEditor> fieldEditors = GetFieldEditors(CreateSut());
        FieldEditor autoSyncEditor = fieldEditors.Single(x =>
            x.PropertyName == nameof(SupportToolsParameters.AutoSyncWithSupportToolsServer));
        FieldEditor timeoutEditor = fieldEditors.Single(x =>
            x.PropertyName == nameof(SupportToolsParameters.SupportToolsServerSyncTimeoutSeconds));

        // Act
        string autoSyncStatus = autoSyncEditor.GetValueStatus(_parameters);
        string timeoutStatus = timeoutEditor.GetValueStatus(_parameters);

        // Assert
        Assert.IsType<BoolFieldEditor>(autoSyncEditor);
        Assert.IsType<IntFieldEditor>(timeoutEditor);
        Assert.Equal(bool.FalseString, autoSyncStatus);
        Assert.Equal("10", timeoutStatus);
    }

    //D3: a new parameters file lists every project, the remote ones too
    [Fact]
    public void HideRemoteOnlyProjectsEditor_WhenParametersAreNew_ShowsOff()
    {
        // Arrange
        FieldEditor editor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.HideRemoteOnlyProjects));

        // Act
        string status = editor.GetValueStatus(_parameters);

        // Assert
        Assert.IsType<BoolFieldEditor>(editor);
        Assert.Equal(bool.FalseString, status);
    }

    //the list shows how many places use every connection, the record menu lists them as menu links
    [Fact]
    public void DatabaseServerConnectionsEditor_WhenSubMenuRequested_ShowsTheUsagesOfEveryConnection()
    {
        // Arrange
        _parameters.DatabaseServerConnections["Conn"] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer, ServerAddress = "TestSqlServer"
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "G1", DevDatabaseParameters = new DatabaseParameters { DbConnectionName = "Conn" }
        };
        FieldEditor connectionsEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.DatabaseServerConnections));

        // Act
        CliMenuSet listMenu = connectionsEditor.GetSubMenu(_parameters)!;
        CliMenuCommand connectionCommand = CliMenuTestAccess.GetMenuItems(listMenu)
            .Single(x => x.MenuItemName == "Conn").CliMenuCommand;
        connectionCommand.CountStatus();
        CliMenuSet connectionMenu = connectionCommand.GetSubMenu()!;

        // Assert
        Assert.Equal("SqlServer: TestSqlServer, Usage count: 1", connectionCommand.StatusString);
        InfoCliMenuCommand place = Assert.Single(CliMenuTestAccess.GetMenuItems(connectionMenu)
            .Select(x => x.CliMenuCommand).OfType<InfoCliMenuCommand>());
        Assert.Equal("G1/App/Dev Database Parameters", place.Name);
        Assert.Equal("G1/App/Dev Database Parameters", place.GetMenuLinkToGo());
    }

    //the list shows how many places use every server, the record menu lists them as menu links
    [Fact]
    public void ServersEditor_WhenSubMenuRequested_ShowsTheUsagesOfEveryServer()
    {
        // Arrange
        _parameters.Servers["Srv"] = new ServerDataModel { IsDecommissioned = true };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ServerInfos = { ["k"] = new ServerInfoModel { ServerName = "Srv", EnvironmentName = "Prod" } }
        };
        FieldEditor serversEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.Servers));

        // Act
        CliMenuSet listMenu = serversEditor.GetSubMenu(_parameters)!;
        CliMenuCommand serverCommand = CliMenuTestAccess.GetMenuItems(listMenu)
            .Single(x => x.MenuItemName == "Srv").CliMenuCommand;
        serverCommand.CountStatus();
        CliMenuSet serverMenu = serverCommand.GetSubMenu()!;

        // Assert
        Assert.Equal("Decommissioned, Usage count: 1", serverCommand.StatusString);
        InfoCliMenuCommand place = Assert.Single(CliMenuTestAccess.GetMenuItems(serverMenu)
            .Select(x => x.CliMenuCommand).OfType<InfoCliMenuCommand>());
        Assert.Equal("G1/App/Srv|Prod", place.Name);
        Assert.Equal("G1/App/Srv|Prod", place.GetMenuLinkToGo());
    }

    //the list shows how many places use every api client, the record menu lists them as menu links
    [Fact]
    public void ApiClientsEditor_WhenSubMenuRequested_ShowsTheUsagesOfEveryApiClient()
    {
        // Arrange
        _parameters.ApiClients["Client"] = new ApiClientSettings { Server = "http://127.0.0.1:0/" };
        _parameters.Servers["Srv"] = new ServerDataModel { WebAgentInstallerName = "Client" };
        FieldEditor apiClientsEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.ApiClients));

        // Act
        CliMenuSet listMenu = apiClientsEditor.GetSubMenu(_parameters)!;
        CliMenuCommand apiClientCommand = CliMenuTestAccess.GetMenuItems(listMenu)
            .Single(x => x.MenuItemName == "Client").CliMenuCommand;
        apiClientCommand.CountStatus();
        CliMenuSet apiClientMenu = apiClientCommand.GetSubMenu()!;

        // Assert
        Assert.Equal("http://127.0.0.1:0/, Usage count: 1", apiClientCommand.StatusString);
        InfoCliMenuCommand place = Assert.Single(CliMenuTestAccess.GetMenuItems(apiClientMenu)
            .Select(x => x.CliMenuCommand).OfType<InfoCliMenuCommand>());
        Assert.Equal("Support Tools Parameters Editor/Servers/Srv/Web Agent Installer Name", place.Name);
        Assert.Equal("Support Tools Parameters Editor/Servers/Srv", place.GetMenuLinkToGo());
    }

    [Fact]
    public void PathMappingsEditor_WhenSubMenuRequested_OpensPathMappingsList()
    {
        // Arrange
        FieldEditor pathMappingsEditor = GetFieldEditors(CreateSut())
            .Single(x => x.PropertyName == nameof(SupportToolsParameters.PathMappings));

        // Act
        CliMenuSet? subMenu = pathMappingsEditor.GetSubMenu(_parameters);

        // Assert
        Assert.Equal("Path Mappings", subMenu?.Caption);
    }

    private SupportToolsParametersEditor CreateSut()
    {
        return new SupportToolsParametersEditor(new Mock<IApplication>().Object, new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parameters, _parametersManager.Object);
    }

    //ParametersEditor keeps its field editors in a protected field
    private static List<FieldEditor> GetFieldEditors(ParametersEditor parametersEditor)
    {
        FieldInfo fieldEditorsField =
            typeof(ParametersEditor).GetField("FieldEditors", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<FieldEditor>)fieldEditorsField.GetValue(parametersEditor)!;
    }
}
