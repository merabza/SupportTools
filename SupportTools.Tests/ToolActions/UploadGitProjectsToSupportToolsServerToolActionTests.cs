using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using Xunit;

namespace SupportTools.Tests.ToolActions;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class UploadGitProjectsToSupportToolsServerToolActionTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";

    //the upload also starts the message hub, which really connects: on port 0 it fails at once
    private const string Server = "http://127.0.0.1:0/api/v1";

    private const string SpaFolderName = "{SpaProjectFolderRelativePath}\\src\\appcarcass";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _rootFolder;
    private readonly StubHttpMessageHandler _server = new();

    public UploadGitProjectsToSupportToolsServerToolActionTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        File.WriteAllText(Path.Combine(_rootFolder, "CSharp.gitignore"), "bin/\r\nobj/\r\n");

        _parameters = new SupportToolsParameters
        {
            FolderForGitignoreFiles = _rootFolder,
            GitIgnorePatterns = { "CSharp" },
            SupportToolsServerWebApiClientName = ApiClientName
        };
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        _parameters.Gits["AppCliTools"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:merabza/AppCliTools.git",
            GitProjectFolderName = "AppCliTools",
            GitIgnorePatternName = "CSharp"
        };

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
        Directory.Delete(_rootFolder, true);
    }

    [Fact]
    public void ActionName_IsTheMenuCaption()
    {
        Assert.Equal("Upload Git Projects To SupportToolsServer",
            UploadGitProjectsToSupportToolsServerToolAction.ActionName);
    }

    [Fact]
    public async Task Run_WhenSupportToolsServerIsNotSpecified_ReturnsFalseWithoutUploading()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Contains("supportToolsServerWebApiClientName does not specified", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Null(_server.LastRequestUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Run_WhenFolderForGitignoreFilesIsNotSpecified_ReturnsFalseWithoutUploading(
        string? folderForGitignoreFiles)
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = folderForGitignoreFiles;

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Contains("FolderForGitignoreFiles is empty", ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_server.LastRequestUri);
    }

    [Fact]
    public async Task Run_WhenServerAccepts_PostsToUploadRouteAndReturnsTrue()
    {
        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(HttpMethod.Post, _server.LastRequestMethod);
        Assert.Equal("/api/v1/git/uploadgitrepos", _server.LastRequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Run_UploadsEveryGitignorePatternWithItsFileContent()
    {
        // Arrange
        _parameters.GitIgnorePatterns.Add("React");
        await File.WriteAllTextAsync(Path.Combine(_rootFolder, "React.gitignore"), "node_modules/\r\n");

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        SyncGitRequest sent = SentRequest();
        Assert.Equal(["CSharp", "React"], sent.GitIgnoreFiles.Select(x => x.Name));
        Assert.Equal(["bin/\r\nobj/\r\n", "node_modules/\r\n"], sent.GitIgnoreFiles.Select(x => x.Content));
    }

    [Fact]
    public async Task Run_WhenGitignoreFileIsMissing_UploadsThePatternWithEmptyContent()
    {
        // Arrange
        _parameters.GitIgnorePatterns.Add("Missing");

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        StsGitIgnoreFileTypeDataModel missing = SentRequest().GitIgnoreFiles.Single(x => x.Name == "Missing");
        Assert.Equal(string.Empty, missing.Content);
    }

    [Fact]
    public async Task Run_UploadsTheRecordNameAndTheFolderNameAsTheyAreInParameters()
    {
        // Arrange
        _parameters.Gits["ReactAppCarcass"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:merabza/ReactAppCarcass.git",
            GitProjectFolderName = SpaFolderName,
            GitIgnorePatternName = "React"
        };

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        StsGitDataModel[] gits = [.. SentRequest().Gits.OrderBy(x => x.GitProjectName, StringComparer.Ordinal)];
        Assert.Equal(["AppCliTools", "ReactAppCarcass"], gits.Select(x => x.GitProjectName));
        Assert.Equal(["AppCliTools", SpaFolderName], gits.Select(x => x.GitProjectFolderName));
        Assert.Equal(["git@github.com:merabza/AppCliTools.git", "git@github.com:merabza/ReactAppCarcass.git"],
            gits.Select(x => x.GitProjectAddress));
        Assert.Equal(["CSharp", "React"], gits.Select(x => x.GitIgnorePatternName));
    }

    [Theory]
    [InlineData(null, "Broken", "CSharp")]
    [InlineData("git@github.com:merabza/Broken.git", " ", "CSharp")]
    [InlineData("git@github.com:merabza/Broken.git", "Broken", "")]
    public async Task Run_WhenARecordIsNotFullyFilled_SkipsItAndUploadsTheRest(string? address, string? folderName,
        string? gitIgnorePatternName)
    {
        // Arrange
        _parameters.Gits["Broken"] = new GitDataModel
        {
            GitProjectAddress = address,
            GitProjectFolderName = folderName,
            GitIgnorePatternName = gitIgnorePatternName
        };

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Contains("Git Repo with key Broken is not fully filled and is not uploaded", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Equal(["AppCliTools"], SentRequest().Gits.Select(x => x.GitProjectName));
    }

    [Fact]
    public async Task Run_WhenServerRefusesTheUpload_ReturnsFalseAndShowsTheError()
    {
        // Arrange
        _server.Respond(HttpStatusCode.Conflict,
            """{"title":"GitAddressIsInUse","status":409,"detail":"Git Address Is Used By AppCliTools"}""");

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Contains("Git Address Is Used By AppCliTools", ConsoleText(), StringComparison.Ordinal);
    }

    private UploadGitProjectsToSupportToolsServerToolAction CreateSut()
    {
        return new UploadGitProjectsToSupportToolsServerToolAction(new Mock<ILogger>().Object,
            _httpClientFactory.Object, _parametersManager.Object, true);
    }

    private SyncGitRequest SentRequest()
    {
        Assert.NotNull(_server.LastRequestBody);
        return JsonSerializer.Deserialize<SyncGitRequest>(_server.LastRequestBody)!;
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
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
