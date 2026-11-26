using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolCommands;
using LibAppInstallWork.ToolCommands.AppSettingsPreparer;
using Microsoft.Extensions.Logging.Abstractions;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.ToolCommands;

//the settings step of Program Updater over a real settings file: the exchange storage is a local folder
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ServiceUpdaterToolCommandTests : IDisposable
{
    private const string ProjectName = "MyService";
    private const string ExchangeName = "Exchange";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _exchangeFolder;
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _sourceFileName;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ServiceUpdaterToolCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _exchangeFolder = Path.Combine(_tempFolder, ExchangeName);
        Directory.CreateDirectory(_exchangeFolder);
        string settingsFolder = Path.Combine(_tempFolder, "Settings");
        Directory.CreateDirectory(settingsFolder);
        _sourceFileName = Path.Combine(settingsFolder, "appsettings.json");
        File.WriteAllText(_sourceFileName,
            """{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:5051" } } }, "VersionInfo": { "AppSettingsVersion": "1.1" } }""");
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task UploadAppSettings_WithoutEncoder_ReturnsTheVersionWrittenIntoTheUploadedFile()
    {
        // Arrange
        AppSettingsPreparerParameters preparerParameters = CreatePreparerParameters(_exchangeFolder);

        // Act
        UploadedAppSettings? result =
            await ServiceUpdaterToolCommand.UploadAppSettings(NullLogger.Instance, preparerParameters, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("appsettings.json", result.FileName);
        string uploadedFileName = Assert.Single(Directory.GetFiles(_exchangeFolder));
        using JsonDocument uploaded = JsonDocument.Parse(await File.ReadAllTextAsync(uploadedFileName));
        string? uploadedVersion = uploaded.RootElement.GetProperty("VersionInfo").GetProperty("AppSettingsVersion")
            .GetString();
        //the upload replaces the version of the source file, so the check can tell the new file from the old one
        Assert.NotEqual("1.1", uploadedVersion);
        Assert.Equal(uploadedVersion, result.Version);
    }

    [Fact]
    public async Task UploadAppSettings_WhenTheExchangeStorageCannotBeCreated_ReturnsNull()
    {
        // Arrange
        string blockingFile = Path.Combine(_tempFolder, "AFile");
        await File.WriteAllTextAsync(blockingFile, "x");
        AppSettingsPreparerParameters preparerParameters =
            CreatePreparerParameters(Path.Combine(blockingFile, ExchangeName));

        // Act
        UploadedAppSettings? result =
            await ServiceUpdaterToolCommand.UploadAppSettings(NullLogger.Instance, preparerParameters, null);

        // Assert
        Assert.Null(result);
    }

    private AppSettingsPreparerParameters CreatePreparerParameters(string exchangeFolder)
    {
        var parameters = new SupportToolsParameters
        {
            ParametersFileDateMask = "yyyyMMddHHmmssfff",
            ParametersFileExtension = ".json",
            SmartSchemaNameForExchange = ExchangeName,
            FileStorageNameForExchange = ExchangeName,
            Projects = { [ProjectName] = new ProjectModel { ProjectType = EProjectType.IsService } },
            //the exchange keeps the newest file
            SmartSchemas =
            {
                [ExchangeName] = new SmartSchema
                {
                    LastPreserveCount = 1,
                    Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 1 }]
                }
            },
            FileStorages = { [ExchangeName] = new FileStorageData { FileStoragePath = exchangeFolder } }
        };
        var serverInfo = new ServerInfoModel
        {
            ServerName = "Srv", EnvironmentName = "Test", AppSettingsJsonSourceFileName = _sourceFileName
        };

        AppSettingsPreparerParameters? preparerParameters =
            AppSettingsPreparerParameters.Create(parameters, ProjectName, serverInfo);
        Assert.NotNull(preparerParameters);
        return preparerParameters;
    }
}
