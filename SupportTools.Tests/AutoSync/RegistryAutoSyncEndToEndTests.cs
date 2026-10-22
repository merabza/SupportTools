using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.AutoSync;
using SupportTools.Tests.Registry.Adapters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.AutoSync;

//auto-sync (D2) as the application wires it: every adapter of the factory, the real ParametersManager writing a
//temp file, the decorator and RegistryAutoSync created through their public constructors, and one fake server
//(FakeSupportToolsServer). Two computers: a change saved on one reaches the other when it starts. All values are
//made up
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RegistryAutoSyncEndToEndTests : IDisposable
{
    private const string BootstrapName = "SupportToolsServer";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly FakeSupportToolsServer _server = new();
    private readonly string _tempFolder;

    public RegistryAutoSyncEndToEndTests()
    {
        _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public async Task TwoComputers_WhenOneSavesAChange_TheOtherTakesItAtItsStart()
    {
        // Arrange
        Computer main = CreateComputer("main");
        main.Parameters.Environments["Production"] = "Production environment";
        await main.Start();
        await main.Save();
        Computer second = CreateComputer("second");
        await second.Start();
        main.Parameters.Environments["Production"] = "Changed on the main computer";
        await main.Save();

        // Act
        Computer secondAgain = second.Restart();
        await secondAgain.Start();

        // Assert
        Assert.Equal("Changed on the main computer", secondAgain.Parameters.Environments["Production"]);
        Assert.Equal("Changed on the main computer", second.LoadFile().Environments["Production"]);
        Assert.Equal(2, _server.VersionOf("environments", "Production"));
        Assert.Equal([
            "[warning] Registry auto-sync: nothing new on SupportToolsServer; run \"Sync Registry With " +
            "SupportToolsServer...\" for 1 to send",
            "Registry auto-sync: sent to SupportToolsServer: Environments/Production",
            "Registry auto-sync: 1 record taken from SupportToolsServer",
            "Registry auto-sync: sent to SupportToolsServer: Environments/Production",
            "Registry auto-sync: 1 record taken from SupportToolsServer"
        ], ConsoleLines());
        Assert.DoesNotContain(BootstrapName, _server.Records("apiclients").Keys);
    }

    //the second and the third save of one operation find nothing to send and make no request
    [Fact]
    public async Task Save_WhenTheSameDataIsSavedAgain_SendsNoRequest()
    {
        // Arrange
        Computer main = CreateComputer("main");
        await main.Start();
        main.Parameters.Environments["Production"] = "Production environment";
        await main.Save();
        int requestCount = _server.Requests.Count;

        // Act
        await main.Save();
        await main.Save();

        // Assert
        Assert.Equal(requestCount, _server.Requests.Count);
        Assert.Equal(1, _server.RequestCount("POST", "environments/update/Production"));
    }

    //the API client of auto-sync is created without the console: a refused record shows only the warning, never the
    //request or its body
    [Fact]
    public async Task Save_WhenServerRefusesTheRecord_ShowsOnlyTheWarning()
    {
        // Arrange
        Computer main = CreateComputer("main");
        await main.Start();
        _server.Fail("POST /api/v1/environments/update/Production", HttpStatusCode.BadRequest, "ValueTooLong");
        main.Parameters.Environments["Production"] = "Production environment";
        int linesBefore = ConsoleLines().Count;

        // Act
        bool saved = await main.Save();

        // Assert
        Assert.True(saved);
        string warning = Assert.Single(ConsoleLines().Skip(linesBefore));
        Assert.StartsWith("[warning] Registry auto-sync: Environments/Production was not sent: ", warning,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Production environment", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //offline: the start warns, the local copy stays, and the saves of the session make no request
    [Fact]
    public async Task Start_WhenServerIsUnreachable_KeepsTheLocalCopyAndTheSavesSendNothing()
    {
        // Arrange
        Computer main = CreateComputer("main");
        main.Parameters.Environments["Production"] = "Production environment";
        _server.IsUnavailable = true;

        // Act
        await main.Start();
        int requestCount = _server.Requests.Count;
        main.Parameters.Environments["Test"] = "Test environment";
        bool saved = await main.Save();

        // Assert
        Assert.True(saved);
        Assert.Equal(requestCount, _server.Requests.Count);
        Assert.Equal("Test environment", main.LoadFile().Environments["Test"]);
        Assert.Contains(ConsoleLines(),
            x => x.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (",
                StringComparison.Ordinal));
    }

    //the template folders are set, as on a real computer: without them the template adapters warn at every sync
    private Computer CreateComputer(string name)
    {
        var parameters = new SupportToolsParameters
        {
            AutoSyncWithSupportToolsServer = true,
            FolderForGitignoreFiles = Path.Combine(_tempFolder, name, "GitIgnore"),
            FolderForEditorConfigFiles = Path.Combine(_tempFolder, name, "EditorConfig"),
            SupportToolsServerWebApiClientName = BootstrapName,
            ApiClients =
            {
                [BootstrapName] = new ApiClientSettings
                {
                    Server = FakeSupportToolsServer.Address, ApiKey = "fake-api-key-of-the-tests"
                }
            }
        };
        return new Computer(Path.Combine(_tempFolder, $"{name}.json"), parameters, _httpClientFactory.Object);
    }

    private List<string> ConsoleLines()
    {
        return [.. _consoleOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)];
    }

    //one computer as Program.cs and SupportToolsServices wire it
    private sealed class Computer
    {
        private readonly RegistryAutoSync _autoSync;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _parametersFileName;
        private readonly AutoSyncParametersManager _parametersManager;

        public Computer(string parametersFileName, SupportToolsParameters parameters,
            IHttpClientFactory httpClientFactory)
        {
            _parametersFileName = parametersFileName;
            _httpClientFactory = httpClientFactory;
            Parameters = parameters;
            var localParametersManager = new SupportToolsMainParametersManager(Options.Create(
                new MainParametersManagerOptions { ParametersFileName = parametersFileName, Par = parameters }));
            _autoSync = new RegistryAutoSync(new Mock<ILogger<RegistryAutoSync>>().Object, httpClientFactory,
                localParametersManager);
            _parametersManager = new AutoSyncParametersManager(localParametersManager, _autoSync);
        }

        public SupportToolsParameters Parameters { get; }

        public Task Start()
        {
            return _autoSync.PullOnStart(false);
        }

        public async Task<bool> Save()
        {
            return await _parametersManager.Save(Parameters, string.Empty);
        }

        //the program starts again with its saved file
        public Computer Restart()
        {
            return new Computer(_parametersFileName, LoadFile(), _httpClientFactory);
        }

        public SupportToolsParameters LoadFile()
        {
            var parametersLoader = new ParametersLoader<SupportToolsParameters>();
            Assert.True(parametersLoader.TryLoadParameters(_parametersFileName));
            return Assert.IsType<SupportToolsParameters>(parametersLoader.Par);
        }
    }
}
