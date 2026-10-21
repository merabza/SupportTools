using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//runs the real git over a throwaway bare remote Lib (a local path, no network) of package project Lib. Consumer App
//(folder {temp}\Projects\App) has its main repository App and its copy of Lib. After the copy was cloned, another
//computer pushed project Lib.B to Lib, so the copy is behind, as a copy of a package git is behind after Distribute all
//packages pushed that git earlier in the same run. App has no project files, so no dotnet command runs
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageDistributorTests : IDisposable
{
    private const string ApiClientName = "LocalPackages";
    private const string Server = "http://127.0.0.1:0/";

    private const string PackagesProps = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="Lib.A" Version="1.0.0" />
            <PackageVersion Include="Lib.B" Version="1.0.1" />
          </ItemGroup>
        </Project>
        """;

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _copyFolder;
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly string _initialCommit;
    private readonly string _mainRepoFolder;
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _remote;
    private readonly RoutingHttpMessageHandler _server = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageDistributorTests()
    {
        _remote = GitTestRepositories.CreateRemote(_tempFolder, "Lib", new Dictionary<string, string>
        {
            [".gitignore"] = "bin/", [@"Lib.A\Lib.A.csproj"] = """<Project Sdk="Microsoft.NET.Sdk" />"""
        });
        string appFolder = Path.Combine(_tempFolder, "Projects", "App");
        _mainRepoFolder = Path.Combine(appFolder, "App");
        Directory.CreateDirectory(_mainRepoFolder);
        File.WriteAllText(Path.Combine(_mainRepoFolder, "Directory.Packages.props"), PackagesProps);
        _copyFolder = Path.Combine(appFolder, "Lib");
        GitTestRepositories.RunGit("clone", _remote, _copyFolder);
        GitTestRepositories.ConfigureRepository(_copyFolder);
        _initialCommit = Head(_copyFolder);

        string otherComputerCopy = Path.Combine(_tempFolder, "OtherComputer", "Lib");
        GitTestRepositories.RunGit("clone", _remote, otherComputerCopy);
        GitTestRepositories.ConfigureRepository(otherComputerCopy);
        Directory.CreateDirectory(Path.Combine(otherComputerCopy, "Lib.B"));
        File.WriteAllText(Path.Combine(otherComputerCopy, "Lib.B", "Lib.B.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk" />""");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "add", "--all");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "commit", "-m", "Lib.B");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "push");

        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        _parameters.Gits["Lib"] = new GitDataModel
        {
            GitProjectAddress = _remote, GitProjectFolderName = "Lib", GitIgnorePatternName = "CSharp"
        };
        _parameters.Gits["App"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/App.git", GitProjectFolderName = "App", GitIgnorePatternName = "CSharp"
        };
        _parameters.Projects["Lib"] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, UseLocalPackages = true, GitProjectNames = ["Lib"]
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = appFolder, UseLocalPackages = true, GitProjectNames = ["App", "Lib"]
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        _server.Respond("GET /v3/index.json",
            """{"resources":[{"@id":"http://127.0.0.1:0/v3/package/","@type":"PackageBaseAddress/3.0.0"}]}""");
        _server.Respond("GET /v3/package/lib.a/index.json", """{"versions":["1.0.0","1.0.3"]}""");
        _server.Respond("GET /v3/package/lib.b/index.json", """{"versions":["1.0.1","1.0.2"]}""");
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    //the copy is pulled before the package versions are raised: Lib.B, which only the pushed commit has, is a package of
    //Lib only in the pulled copy
    [Fact]
    public async Task DistributePackage_WhenTheCopyOfThePackageGitIsBehind_PullsItBeforeRaisingThePackageVersions()
    {
        // Act
        bool result = await CreateSut().DistributePackage();

        // Assert
        Assert.True(result);
        Assert.Equal(Head(_remote), Head(_copyFolder));
        string packagesProps = ReadPackagesProps();
        Assert.Contains("""<PackageVersion Include="Lib.A" Version="1.0.3" />""", packagesProps,
            StringComparison.Ordinal);
        Assert.Contains("""<PackageVersion Include="Lib.B" Version="1.0.2" />""", packagesProps,
            StringComparison.Ordinal);
        Assert.DoesNotContain("[ERROR]", ConsoleText(), StringComparison.Ordinal);
    }

    //the local changes belong to the user: the copy stays as it is and the distribution goes on with it
    [Fact]
    public async Task DistributePackage_WhenTheCopyHasLocalChanges_DoesNotPullItAndWarns()
    {
        // Arrange
        string localFile = Path.Combine(_copyFolder, "Local.txt");
        await File.WriteAllTextAsync(localFile, "local");

        // Act
        bool result = await CreateSut().DistributePackage();

        // Assert
        Assert.True(result);
        Assert.Equal(_initialCommit, Head(_copyFolder));
        Assert.True(File.Exists(localFile));
        Assert.Contains($"Package repository {_copyFolder} has local changes and was not pulled for project App",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("""<PackageVersion Include="Lib.B" Version="1.0.1" />""", ReadPackagesProps(),
            StringComparison.Ordinal);
    }

    //a copy with a commit of its own cannot be fast-forwarded: the consumer is skipped before its versions change
    [Fact]
    public async Task DistributePackage_WhenTheCopyCannotBeFastForwarded_SkipsTheConsumer()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_copyFolder, "Local.txt"), "local");
        GitTestRepositories.RunGit("-C", _copyFolder, "add", "--all");
        GitTestRepositories.RunGit("-C", _copyFolder, "commit", "-m", "Local");
        string localCommit = Head(_copyFolder);

        // Act
        bool result = await CreateSut().DistributePackage();

        // Assert
        Assert.False(result);
        Assert.Equal(localCommit, Head(_copyFolder));
        Assert.Contains($"Cannot pull package repository {_copyFolder} with --ff-only for project App, skipped",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
        Assert.Equal(PackagesProps, ReadPackagesProps());
    }

    private PackageDistributor CreateSut()
    {
        return new PackageDistributor(new Mock<ILogger>().Object, _httpClientFactory.Object, _parametersManager.Object,
            "Lib");
    }

    private static string Head(string repository)
    {
        return GitTestRepositories.RunGit("-C", repository, "rev-parse", "HEAD");
    }

    private string ReadPackagesProps()
    {
        return File.ReadAllText(Path.Combine(_mainRepoFolder, "Directory.Packages.props"));
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    //the local package manager: the registered routes answer with JSON, the rest with 404
    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = [];

        public List<string> Requests { get; } = [];

        public void Respond(string request, string body)
        {
            _responses[request] = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add(key);

            if (!_responses.TryGetValue(key, out string? body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request, Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
