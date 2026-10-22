using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.DistributeAllPackages;

//runs the real git over the repository of package project Pkg ({temp}\Pkg\Pkg: solution Pkg.slnx with projects Pkg.A,
//Pkg.B and Pkg.Tests, one commit) and asks a fake package server. The server has the packages Pkg.A and Pkg.B of the
//last upload 1.0.2, built from the commit the repository is at; Pkg.Tests is not a package. The tests change one side
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UploadedPackageCheckerTests : IDisposable
{
    private const string ApiClientName = "LocalPackages";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly string _repository;
    private readonly RoutingHttpMessageHandler _server = new();
    private readonly UploadedPackageChecker _sut;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly string _uploadedCommitId;

    public UploadedPackageCheckerTests()
    {
        _repository = GitTestRepositories.InitRepository(Path.Combine(_tempFolder, "Pkg", "Pkg"));
        File.WriteAllText(Path.Combine(_repository, ".gitignore"), "bin/");
        File.WriteAllText(Path.Combine(_repository, "Pkg.slnx"), "<Solution />");
        WriteProjectFile("Pkg.A");
        WriteProjectFile("Pkg.B");
        WriteProjectFile("Pkg.Tests");
        _uploadedCommitId = CommitAll("Initial");

        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/" };
        _parameters.Projects["Pkg"] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, SolutionFileName = Path.Combine(_repository, "Pkg.slnx")
        };
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        _server.Respond("GET /v3/index.json",
            """{"resources":[{"@id":"http://127.0.0.1:0/v3/package/","@type":"PackageBaseAddress/3.0.0"}]}""");
        _server.Respond("GET /v3/package/pkg.a/index.json", """{"versions":["1.0.1","1.0.2"]}""");
        _server.Respond("GET /v3/package/pkg.b/index.json", """{"versions":["1.0.2"]}""");
        _server.Respond("GET /v3/package/pkg.a/1.0.2/pkg.a.nuspec",
            Nuspec($"""<repository type="git" commit="{_uploadedCommitId}" />"""));
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));

        _sut = new UploadedPackageChecker(new Mock<ILogger>().Object, httpClientFactory.Object,
            parametersManager.Object);

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

    //the versions of every project of the repository are read, the commit only from the first package
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheRepositoryIsAtTheUploadedCommit_ReturnsTheUploadedVersion()
    {
        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Equal("1.0.2", result);
        Assert.Equal([
            "GET /v3/index.json", "GET /v3/package/pkg.a/index.json", "GET /v3/package/pkg.b/index.json",
            "GET /v3/package/pkg.tests/index.json", "GET /v3/package/pkg.a/1.0.2/pkg.a.nuspec"
        ], _server.Requests);
        Assert.Equal(string.Empty, ConsoleText());
    }

    //build outputs are ignored by git, so they are no change
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenOnlyIgnoredFilesWereAdded_ReturnsTheUploadedVersion()
    {
        // Arrange
        string binFolder = Directory.CreateDirectory(Path.Combine(_repository, "Pkg.A", "bin")).FullName;
        await File.WriteAllTextAsync(Path.Combine(binFolder, "Pkg.A.dll"), "binary");

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Equal("1.0.2", result);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheRepositoryHasNewCommits_ReturnsNull()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_repository, "Pkg.A", "Class.cs"), "class C;");
        string headCommitId = CommitAll("Change");

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Contains(
            $"Project Pkg has changed since its packages 1.0.2 were uploaded (commit {_uploadedCommitId[..7]} -> {headCommitId[..7]}), they will be built and uploaded",
            ConsoleText(), StringComparison.Ordinal);
    }

    //an uncommitted change, a new file too, is not in the uploaded packages, so the server is not asked
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenThereAreUncommittedChanges_ReturnsNullWithoutAskingTheServer()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_repository, "Pkg.A", "Class.cs"), "class C;");

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Empty(_server.Requests);
        Assert.Contains("Project Pkg has uncommitted changes, its packages will be built and uploaded", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenNoPackageIsOnTheServer_ReturnsNull()
    {
        // Arrange
        _server.Respond("GET /v3/package/pkg.a/index.json", string.Empty, HttpStatusCode.NotFound);
        _server.Respond("GET /v3/package/pkg.b/index.json", """{"versions":[]}""");

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.DoesNotContain(_server.Requests, x => x.EndsWith(".nuspec", StringComparison.Ordinal));
        Assert.Contains("Packages of project Pkg are not on the package server yet, they will be built and uploaded",
            ConsoleText(), StringComparison.Ordinal);
    }

    //the last upload failed half way, or a project is no package any more: the packages are not of one upload
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenThePackagesHaveDifferentLatestVersions_ReturnsNull()
    {
        // Arrange
        _server.Respond("GET /v3/package/pkg.b/index.json", """{"versions":["1.0.1"]}""");

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.DoesNotContain(_server.Requests, x => x.EndsWith(".nuspec", StringComparison.Ordinal));
        Assert.Contains(
            "Packages of project Pkg have different latest versions on the package server (Pkg.A 1.0.2, Pkg.B 1.0.1), they will be built and uploaded",
            ConsoleText(), StringComparison.Ordinal);
    }

    //a package packed without SourceLink does not say what it was built from
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheNuspecNamesNoCommit_ReturnsNull()
    {
        // Arrange
        _server.Respond("GET /v3/package/pkg.a/1.0.2/pkg.a.nuspec", Nuspec(string.Empty));

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Contains(
            "Package Pkg.A 1.0.2 does not name its git commit, packages of project Pkg will be built and uploaded",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheServerFails_WarnsAndReturnsNull()
    {
        // Arrange
        _server.Respond("GET /v3/package/pkg.b/index.json", string.Empty, HttpStatusCode.InternalServerError);

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        string output = ConsoleText();
        Assert.Contains(
            "[warning] Cannot check the last upload of project Pkg, its packages will be built and uploaded: ", output,
            StringComparison.Ordinal);
        Assert.Contains("500", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheServerCannotBeReached_WarnsAndReturnsNull()
    {
        // Arrange
        _server.IsUnreachable = true;

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Contains(
            "[warning] Cannot check the last upload of project Pkg, its packages will be built and uploaded: No connection could be made",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheServiceIndexCannotBeRead_ReturnsNull()
    {
        // Arrange
        _server.Respond("GET /v3/index.json", string.Empty, HttpStatusCode.NotFound);

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Equal(["GET /v3/index.json"], _server.Requests);
        Assert.Contains("Cannot get nuget service index http://127.0.0.1:0/v3/index.json. Status code is NotFound",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheSolutionFolderIsNotARepository_WarnsAndReturnsNull()
    {
        // Arrange
        string folder = Directory.CreateDirectory(Path.Combine(_tempFolder, "NotARepository")).FullName;
        SetSolutionFileName(Path.Combine(folder, "Pkg.slnx"));

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Empty(_server.Requests);
        Assert.Contains(
            "[warning] Cannot check the last upload of project Pkg, its packages will be built and uploaded: ",
            ConsoleText(), StringComparison.Ordinal);
    }

    //the build steps report a missing solution file or package server themselves
    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenTheProjectHasNoSolutionFile_ReturnsNullWithoutAWord()
    {
        // Arrange
        SetSolutionFileName(null);

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Empty(_server.Requests);
        Assert.Equal(string.Empty, ConsoleText());
    }

    [Fact]
    public async Task GetUnchangedUploadedVersion_WhenThePackageServerIsNotConfigured_ReturnsNullWithoutAWord()
    {
        // Arrange
        _parameters.LocalPackageManagerWebApiClientName = "Missing";

        // Act
        string? result = await _sut.GetUnchangedUploadedVersion("Pkg", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Empty(_server.Requests);
        Assert.Equal(string.Empty, ConsoleText());
    }

    private void WriteProjectFile(string projectName)
    {
        string projectFolder = Directory.CreateDirectory(Path.Combine(_repository, projectName)).FullName;
        File.WriteAllText(Path.Combine(projectFolder, $"{projectName}.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk" />""");
    }

    private void SetSolutionFileName(string? solutionFileName)
    {
        _parameters.Projects["Pkg"] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, SolutionFileName = solutionFileName
        };
    }

    private string CommitAll(string message)
    {
        GitTestRepositories.RunGit("-C", _repository, "add", "--all");
        GitTestRepositories.RunGit("-C", _repository, "commit", "-m", message);
        return GitTestRepositories.RunGit("-C", _repository, "rev-parse", "HEAD");
    }

    //the nuspec of Pkg.A 1.0.2 as BaGetter sends it: with the byte order mark
    private static string Nuspec(string repositoryElement)
    {
        return "﻿" + $"""
                      <?xml version="1.0" encoding="utf-8"?>
                      <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                        <metadata>
                          <id>Pkg.A</id>
                          <version>1.0.2</version>
                          {repositoryElement}
                        </metadata>
                      </package>
                      """;
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    //the local package manager: the registered routes answer with their status and body, the rest with 404
    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<string> Requests { get; } = [];
        public bool IsUnreachable { get; set; }

        public void Respond(string request, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responses[request] = (statusCode, body);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add(key);

            if (IsUnreachable)
            {
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("No connection could be made"));
            }

            (HttpStatusCode statusCode, string body) =
                _responses.GetValueOrDefault(key, (HttpStatusCode.NotFound, string.Empty));
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                RequestMessage = request, Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
