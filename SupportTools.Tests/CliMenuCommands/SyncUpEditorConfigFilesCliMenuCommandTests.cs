using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using SupportTools.CliMenuCommands;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncUpEditorConfigFilesCliMenuCommandTests : IDisposable
{
    private const string MenuName = "Sync Up .editorconfig files...";
    private const string ApiClientName = "SupportToolsServer";

    private readonly List<(string FieldName, bool DefaultValue)> _boolPrompts = [];
    private readonly EditorConfigTestEnvironment _env = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly StubHttpMessageHandler _server = new();
    private bool _confirm = true;

    public SyncUpEditorConfigFilesCliMenuCommandTests()
    {
        _env.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _env.Parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://localhost:5033/api/v1" };
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_server, false));
    }

    public void Dispose()
    {
        _server.Dispose();
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        SyncUpEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(MenuName, sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_SetsMenuName()
    {
        // Act
        var sut = new SyncUpEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object);

        // Assert
        Assert.Equal(MenuName, sut.Name);
    }

    [Fact]
    public async Task RunBody_WhenNotConfirmed_ReturnsFalseWithoutUploading()
    {
        // Arrange
        _confirm = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        (string fieldName, bool defaultValue) = Assert.Single(_boolPrompts);
        Assert.Contains(".editorconfig records", fieldName, StringComparison.Ordinal);
        Assert.False(defaultValue);
        Assert.Null(_server.LastRequestUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunBody_WhenFolderForEditorConfigFilesIsNotSpecified_ReturnsFalseWithoutUploading(
        string? folderForEditorConfigFiles)
    {
        // Arrange
        _env.Parameters.FolderForEditorConfigFiles = folderForEditorConfigFiles;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("FolderForEditorConfigFiles is empty", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_server.LastRequestUri);
    }

    //the server deletes the patterns that are missing from the upload, so one missing file stops the whole upload
    [Fact]
    public async Task RunBody_WhenATemplateFileIsMissing_ReturnsFalseWithoutUploading()
    {
        // Arrange
        _env.Parameters.EditorConfigPatterns.Add("React");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains(Path.Combine(_env.TemplatesFolder, "React.editorconfig"), _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Null(_server.LastRequestUri);
    }

    [Fact]
    public async Task RunBody_WhenSupportToolsServerIsNotSpecified_ReturnsFalseWithoutUploading()
    {
        // Arrange
        _env.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("supportToolsServerApiClient is null", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_server.LastRequestUri);
    }

    [Fact]
    public async Task RunBody_WhenConfirmed_UploadsEveryTemplateWithoutMerge()
    {
        // Arrange
        const string reactContent = "root = true\r\n\r\n[*.ts]\r\nindent_size = 2\r\n";
        await File.WriteAllTextAsync(Path.Combine(_env.TemplatesFolder, "React.editorconfig"), reactContent);
        _env.Parameters.EditorConfigPatterns.Add("React");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(HttpMethod.Post, _server.LastRequestMethod);
        Assert.Equal("/api/v1/git/syncupeditorconfigfiletypes/False", _server.LastRequestUri!.AbsolutePath);
        List<StsEditorConfigFileTypeDataModel> sent =
            JsonSerializer.Deserialize<List<StsEditorConfigFileTypeDataModel>>(_server.LastRequestBody!)!;
        Assert.Equal([EditorConfigTestEnvironment.PatternName, "React"], sent.Select(x => x.Name));
        Assert.Equal([EditorConfigTestEnvironment.TemplateContent, reactContent], sent.Select(x => x.Content));
        Assert.Contains("2 .editorconfig files uploaded to server", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenServerRefusesTheUpload_ReturnsFalseAndShowsTheError()
    {
        // Arrange
        _server.Respond(HttpStatusCode.BadRequest,
            """{"title":"ValueTooLong","status":400,"detail":"CSharp.Content Is Longer Than 65536 Characters"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("CSharp.Content Is Longer Than 65536 Characters", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain("uploaded to server", _env.ConsoleText(), StringComparison.Ordinal);
    }

    private SyncUpEditorConfigFilesCliMenuCommand CreateSut()
    {
        return new SyncUpEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object, InputBool);
    }

    private bool InputBool(string fieldName, bool defaultValue)
    {
        _boolPrompts.Add((fieldName, defaultValue));
        return _confirm;
    }

    //remembers the last request and answers with the response given in advance (200 without a body by default)
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private string? _body;
        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        public Uri? LastRequestUri { get; private set; }
        public HttpMethod? LastRequestMethod { get; private set; }
        public string? LastRequestBody { get; private set; }

        public void Respond(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestMethod = request.Method;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(_statusCode) { RequestMessage = request };
            if (_body is not null)
            {
                response.Content = new StringContent(_body, Encoding.UTF8, "application/problem+json");
            }

            return response;
        }
    }
}
