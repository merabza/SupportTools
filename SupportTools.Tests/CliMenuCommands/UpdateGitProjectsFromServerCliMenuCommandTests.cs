using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

//local gits: CrawlerConsole, SystemTools, RepoA. The server also has OnlyOnServer, and SystemTools.DependencyInjection
//in both CrawlerConsole and SystemTools (as in the real data)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateGitProjectsFromServerCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string Server = "http://127.0.0.1:0/api/v1";
    private const string GitProjectsPath = "GET /api/v1/git/gitprojects";

    private const string ServerGitProjectsJson = """
        [
          {"gitName":"CrawlerConsole","projectRelativePath":"CrawlerConsole\\SystemTools.DependencyInjection","projectFileName":"SystemTools.DependencyInjection.csproj","dependsOnProjectNames":[]},
          {"gitName":"OnlyOnServer","projectRelativePath":"OnlyOnServer\\AppX","projectFileName":"AppX.csproj","dependsOnProjectNames":[]},
          {"gitName":"RepoA","projectRelativePath":"RepoA\\AppA","projectFileName":"AppA.csproj","dependsOnProjectNames":["LibA","SystemTools.DependencyInjection"]},
          {"gitName":"RepoA","projectRelativePath":"RepoA\\LibA","projectFileName":"LibA.csproj","dependsOnProjectNames":[]},
          {"gitName":"SystemTools","projectRelativePath":"SystemTools\\SystemTools.DependencyInjection","projectFileName":"SystemTools.DependencyInjection.csproj","dependsOnProjectNames":["SystemTools.SystemToolsShared"]}
        ]
        """;

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly RoutingHttpMessageHandler _server = new();

    public UpdateGitProjectsFromServerCliMenuCommandTests()
    {
        _parameters = new SupportToolsParameters { SupportToolsServerWebApiClientName = ApiClientName };
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        foreach (string gitName in (string[])["CrawlerConsole", "SystemTools", "RepoA"])
        {
            _parameters.Gits[gitName] = new GitDataModel
            {
                GitProjectAddress = $"git@github.com:x/{gitName}.git",
                GitProjectFolderName = gitName,
                GitIgnorePatternName = "CSharp"
            };
        }

        //the result of an earlier local Update Git Projects
        _parameters.GitProjects["AppA"] = new GitProjectDataModel
        {
            GitName = "RepoA",
            ProjectRelativePath = @"RepoA\AppA",
            ProjectFileName = "AppA.csproj",
            DependsOnProjectNames = ["SystemTools.DependencyInjection", "LibA"]
        };
        _parameters.GitProjects["Removed"] = new GitProjectDataModel
        {
            GitName = "RepoA", ProjectRelativePath = @"RepoA\Removed", ProjectFileName = "Removed.csproj"
        };
        _server.Respond(GitProjectsPath, HttpStatusCode.OK, ServerGitProjectsJson);

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
    }

    private UpdateGitProjectsFromServerCliMenuCommand CreateSut()
    {
        return new UpdateGitProjectsFromServerCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object);
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    private void VerifyNotSaved()
    {
        _parametersManager.Verify(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        UpdateGitProjectsFromServerCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Update Git Projects From SupportToolsServer", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    //The result equals the local Update Git Projects: the projects of the local gits only, the later git keeps a
    //repeated name, the projects that the server does not have are removed
    [Fact]
    public async Task RunBody_WhenServerHasGitProjects_ReplacesTheLocalOnesAndSavesTheRoot()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(["AppA", "LibA", "SystemTools.DependencyInjection"], _parameters.GitProjects.Keys.Order());
        GitProjectDataModel dependencyInjection = _parameters.GitProjects["SystemTools.DependencyInjection"];
        Assert.Equal("SystemTools", dependencyInjection.GitName);
        Assert.Equal(@"SystemTools\SystemTools.DependencyInjection", dependencyInjection.ProjectRelativePath);
        Assert.Equal(["SystemTools.SystemToolsShared"], dependencyInjection.DependsOnProjectNames);
        Assert.Equal(["LibA", "SystemTools.DependencyInjection"],
            _parameters.GitProjects["AppA"].DependsOnProjectNames);
        _parametersManager.Verify(x => x.Save(_parameters, It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal([GitProjectsPath], _server.Requests);
    }

    //The skipped git, the repeated name and the counts are shown; the dependencies of AppA only changed their order
    [Fact]
    public async Task RunBody_WhenServerHasGitProjects_ReportsTheSkippedGitTheDuplicateAndTheCounts()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string console = ConsoleText();
        Assert.Contains("Git OnlyOnServer is not in the local Gits, its projects are skipped", console,
            StringComparison.Ordinal);
        Assert.Contains(
            "Git project SystemTools.DependencyInjection is in CrawlerConsole and in SystemTools, the one of SystemTools is used",
            console, StringComparison.Ordinal);
        Assert.Contains("3 git projects received from SupportToolsServer: 2 added, 0 changed, 1 removed", console,
            StringComparison.Ordinal);
    }

    //Before the server has scanned its clones the list is empty: replacing the local projects with it would lose them
    [Fact]
    public async Task RunBody_WhenServerHasNoGitProjects_KeepsTheLocalOnes()
    {
        // Arrange
        _server.Respond(GitProjectsPath, HttpStatusCode.OK, "[]");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["AppA", "Removed"], _parameters.GitProjects.Keys.Order());
        Assert.Contains("SupportToolsServer has no git projects yet", ConsoleText(), StringComparison.Ordinal);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenServerFails_KeepsTheLocalGitProjects()
    {
        // Arrange
        _server.Respond(GitProjectsPath, HttpStatusCode.InternalServerError,
            """{"title":"Db","status":500,"detail":"Database failure"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["AppA", "Removed"], _parameters.GitProjects.Keys.Order());
        Assert.Contains("Database failure", ConsoleText(), StringComparison.Ordinal);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenSupportToolsServerIsNotSpecified_ReturnsFalseWithoutCallingServer()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("supportToolsServerApiClient is null", ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
        VerifyNotSaved();
    }

    //A local git without its address is not scanned locally either (GitRepos.Create leaves it out)
    [Fact]
    public async Task RunBody_WhenALocalGitIsIncomplete_LeavesItsProjectsOut()
    {
        // Arrange
        _parameters.Gits["RepoA"].GitProjectAddress = null;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(["SystemTools.DependencyInjection"], _parameters.GitProjects.Keys);
        Assert.Contains("Git RepoA is not in the local Gits", ConsoleText(), StringComparison.Ordinal);
    }

    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<string> Requests { get; } = [];

        public void Respond(string request, HttpStatusCode statusCode, string body)
        {
            _responses[request] = (statusCode, body);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add(key);

            if (!_responses.TryGetValue(key, out (HttpStatusCode StatusCode, string Body) response))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
            }

            return Task.FromResult(new HttpResponseMessage(response.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(response.Body, Encoding.UTF8,
                    response.StatusCode == HttpStatusCode.OK ? "application/json" : "application/problem+json")
            });
        }
    }
}
