using System;
using System.Collections.Generic;
using System.IO;
using LibAppInstallWork;
using Newtonsoft.Json;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ServiceInstallScriptCreator;

public sealed class KestrelPortsReaderTests
{
    [Fact]
    public void Read_WithHttpEndpoint_ReturnsItsPort()
    {
        const string json = """{ "Logging": {}, "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:5051" } } } }""";

        Assert.Equal([5051], KestrelPortsReader.Read(json));
    }

    [Theory]
    [InlineData("http://+:5022", 5022)]
    [InlineData("http://0.0.0.0:5023/", 5023)]
    [InlineData("https://localhost:443/api", 443)]
    [InlineData("http://[::]:5070", 5070)]
    [InlineData(" http://*:5080 ", 5080)]
    public void Read_WithUrlForms_ReturnsPort(string kestrelAddress, int expected)
    {
        string json = JsonConvert.SerializeObject(new
        {
            Kestrel = new { Endpoints = new Dictionary<string, object> { ["Http"] = new { Url = kestrelAddress } } }
        });

        Assert.Equal([expected], KestrelPortsReader.Read(json));
    }

    [Fact]
    public void Read_WithSeveralEndpoints_ReturnsDistinctPortsInOrder()
    {
        const string json = """
                            { "Kestrel": { "Endpoints": {
                              "Http": { "Url": "http://*:5031" },
                              "Https": { "Url": "https://*:5443" },
                              "Other": { "Url": "http://localhost:5031" } } } }
                            """;

        Assert.Equal([5031, 5443], KestrelPortsReader.Read(json));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "Kestrel": {} }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": [] } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": "http://*:5051" } } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": { "Url": 5051 } } } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://localhost" } } } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:0" } } } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:65536" } } } }""")]
    [InlineData("""{ "Kestrel": { "Endpoints": { "Http": { "Url": "*:5051" } } } }""")]
    public void Read_WithoutUsablePort_ReturnsEmpty(string json)
    {
        Assert.Empty(KestrelPortsReader.Read(json));
    }

    [Fact]
    public void Read_WithInvalidJson_Throws()
    {
        Assert.ThrowsAny<JsonReaderException>(() => KestrelPortsReader.Read("{ not json"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void ReadFile_WithoutFileName_ReturnsNullAndReason(string? fileName)
    {
        Assert.Null(KestrelPortsReader.ReadFile(fileName, out string? error));
        Assert.Equal("App Settings Json Source File Name is not specified", error);
    }

    [Fact]
    public void ReadFile_WithMissingFile_ReturnsNullAndReason()
    {
        string fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        Assert.Null(KestrelPortsReader.ReadFile(fileName, out string? error));
        Assert.Equal($"App Settings Json Source File {fileName} does not exist", error);
    }

    [Fact]
    public void ReadFile_WithInvalidJson_ReturnsNullAndReason()
    {
        using var file = new TempJsonFile("{ not json");

        Assert.Null(KestrelPortsReader.ReadFile(file.FileName, out string? error));
        Assert.StartsWith($"App Settings Json Source File {file.FileName} is not valid json: ", error,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReadFile_WithKestrelEndpoint_ReturnsItsPort()
    {
        using var file = new TempJsonFile(KestrelJson);

        Assert.Equal([5031], KestrelPortsReader.ReadFile(file.FileName, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void GetPortsToOpen_WhenOpenPortIsOff_ReturnsEmptyWithoutReadingTheFile()
    {
        var serverInfo = new ServerInfoModel { OpenPort = false, AppSettingsJsonSourceFileName = "missing.json" };

        Assert.Equal([], KestrelPortsReader.GetPortsToOpen(serverInfo, "App"));
    }

    [Fact]
    public void GetPortsToOpen_WhenOpenPortIsOn_ReturnsThePortsOfTheFile()
    {
        using var file = new TempJsonFile(KestrelJson);
        var serverInfo = new ServerInfoModel { OpenPort = true, AppSettingsJsonSourceFileName = file.FileName };

        Assert.Equal([5031], KestrelPortsReader.GetPortsToOpen(serverInfo, "App"));
    }

    private const string KestrelJson = """{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:5031" } } } }""";

    private sealed class TempJsonFile : IDisposable
    {
        public TempJsonFile(string content)
        {
            FileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(FileName, content);
        }

        public string FileName { get; }

        public void Dispose()
        {
            File.Delete(FileName);
        }
    }
}
