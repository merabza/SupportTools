using System;
using System.IO;
using System.Threading.Tasks;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.FieldEditors;

//OpenPort is a Yes/No choice whose prompt and status show the port of the App Settings Json Source File
public sealed class OpenPortFieldEditorTests : IDisposable
{
    private readonly string _appSettingsFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        File.Delete(_appSettingsFileName);
    }

    [Fact]
    public async Task UpdateField_WhenCalled_AsksWithThePortAndStoresTheAnswer()
    {
        // Arrange
        await File.WriteAllTextAsync(_appSettingsFileName,
            """{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://*:5031" } } } }""");
        var serverInfo = new ServerInfoModel { AppSettingsJsonSourceFileName = _appSettingsFileName };
        string? askedPrompt = null;
        bool askedDefault = true;
        var sut = new OpenPortFieldEditor(nameof(ServerInfoModel.OpenPort), (prompt, defaultValue) =>
        {
            askedPrompt = prompt;
            askedDefault = defaultValue;
            return true;
        });

        // Act
        await sut.UpdateField(null, serverInfo);

        // Assert
        Assert.Equal(("Open Port (port 5031)", false), (askedPrompt, askedDefault));
        Assert.True(serverInfo.OpenPort);
        Assert.Equal("Yes (port 5031)", sut.GetValueStatus(serverInfo));
    }

    [Fact]
    public void GetValueStatus_WhenFileHasNoKestrelPort_SaysSo()
    {
        File.WriteAllText(_appSettingsFileName, "{}");
        var serverInfo = new ServerInfoModel { AppSettingsJsonSourceFileName = _appSettingsFileName };

        Assert.Equal("No (no Kestrel endpoint port)", CreateSut().GetValueStatus(serverInfo));
    }

    [Fact]
    public void GetValueStatus_WhenFileIsMissing_ShowsTheReason()
    {
        var serverInfo = new ServerInfoModel { OpenPort = true, AppSettingsJsonSourceFileName = _appSettingsFileName };

        Assert.Equal($"Yes (App Settings Json Source File {_appSettingsFileName} does not exist)",
            CreateSut().GetValueStatus(serverInfo));
    }

    private static OpenPortFieldEditor CreateSut()
    {
        return new OpenPortFieldEditor(nameof(ServerInfoModel.OpenPort),
            (_, _) => throw new InvalidOperationException("no input expected"));
    }
}
