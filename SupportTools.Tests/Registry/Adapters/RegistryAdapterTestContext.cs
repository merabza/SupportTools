using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using Moq;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;

namespace SupportTools.Tests.Registry.Adapters;

//the fake server, an API client of it and the parameters of one computer with its warnings. The client is created
//without the console, so a failed request prints nothing; a DELETE still starts the message hub, which fails at once
//on port 0 and writes to the console, so the context captures the console output: the test classes that use it
//belong to ConsoleCaptureCollection
internal sealed class RegistryAdapterTestContext : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private string? _tempFolder;

    public RegistryAdapterTestContext()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(Server, false));
        ApiClient = new SupportToolsServerApiClient(null, httpClientFactory.Object, FakeSupportToolsServer.Address,
            null, false);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public FakeSupportToolsServer Server { get; } = new();
    public SupportToolsServerApiClient ApiClient { get; }
    public SupportToolsParameters Parameters { get; } = new();
    public RegistrySyncWarnings Warnings { get; } = new();
    public PathMapper PathMapper { get; set; } = MapperTestHelpers.WindowsPathMapper();

    //everything written to the console since the context was created
    public string ConsoleOutput => _consoleOutput.ToString();

    //a temp folder, created on first use and deleted with the context
    public string TempFolder => _tempFolder ??= Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Server.Dispose();
        if (_tempFolder is not null)
        {
            Directory.Delete(_tempFolder, true);
        }
    }
}
