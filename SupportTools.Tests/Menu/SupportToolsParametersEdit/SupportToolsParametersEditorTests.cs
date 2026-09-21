using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
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
        nameof(SupportToolsParameters.DotnetTools),
        nameof(SupportToolsParameters.SupportToolsServerWebApiClientName),
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
        nameof(SupportToolsParameters.ScaffoldSeedersWorkFolder),
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
