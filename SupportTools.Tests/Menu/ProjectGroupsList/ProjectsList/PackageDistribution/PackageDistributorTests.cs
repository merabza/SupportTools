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
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//runs the real git over a throwaway bare remote Lib (a local path, no network) of package project Lib. Consumer App
//(folder {temp}\Projects\App) has its main repository App and its copy of Lib. After the copy was cloned, another
//computer pushed project Lib.B to Lib, so the copy is behind, as a copy of a package git is behind after Distribute all
//packages pushed that git earlier in the same run. App has a project file only in the tests whose csproj changes are a
//fake, so no dotnet command runs
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

    private readonly string _appFolder;
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
        _appFolder = Path.Combine(_tempFolder, "Projects", "App");
        _mainRepoFolder = Path.Combine(_appFolder, "App");
        Directory.CreateDirectory(_mainRepoFolder);
        File.WriteAllText(Path.Combine(_mainRepoFolder, "Directory.Packages.props"), PackagesProps);

        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        _parameters.Gits["App"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/App.git",
            GitProjectFolderName = "App",
            GitIgnorePatternName = "CSharp"
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _appFolder, UseLocalPackages = true, GitProjectNames = ["App"]
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        _remote = AddPackageGitBehindItsRemote("Lib");
        _copyFolder = Path.Combine(_appFolder, "Lib");
        _initialCommit = Head(_copyFolder);

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
        bool result = await CreateSut().DistributePackage("Lib");

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
        bool result = await CreateSut().DistributePackage("Lib");

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
        bool result = await CreateSut().DistributePackage("Lib");

        // Assert
        Assert.False(result);
        Assert.Equal(localCommit, Head(_copyFolder));
        Assert.Contains($"Cannot pull package repository {_copyFolder} with --ff-only for project App, skipped",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
        Assert.Equal(PackagesProps, ReadPackagesProps());
    }

    //App takes Lib and then Ext, which come one after the other in Distribute all packages. The copies of both are
    //pulled first, so the restore after the swap of Lib already sees the pulled copy of Ext too: a pulled copy next to
    //one that is still old asks for different versions of the same package (NU1605)
    [Fact]
    public async Task DistributePackagesToProject_WhenSeveralCopiesAreBehind_PullsThemAllBeforeTheFirstRestore()
    {
        // Arrange
        string extRemote = AddPackageGitBehindItsRemote("Ext");
        string extCopyFolder = Path.Combine(_appFolder, "Ext");
        await File.WriteAllTextAsync(Path.Combine(_mainRepoFolder, "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Lib.B" Version="1.0.1" />
                <PackageVersion Include="Ext.B" Version="1.0.1" />
              </ItemGroup>
            </Project>
            """);
        string projectFile = Path.Combine(_mainRepoFolder, "App.csproj");
        await File.WriteAllTextAsync(projectFile, """
                                                  <Project Sdk="Microsoft.NET.Sdk">
                                                    <ItemGroup>
                                                      <ProjectReference Include="..\Lib\Lib.A\Lib.A.csproj" />
                                                      <ProjectReference Include="..\Ext\Ext.A\Ext.A.csproj" />
                                                    </ItemGroup>
                                                  </Project>
                                                  """);
        _server.Respond("GET /v3/package/ext.a/index.json", """{"versions":["1.0.0","1.0.5"]}""");
        _server.Respond("GET /v3/package/ext.b/index.json", """{"versions":["1.0.1","1.0.4"]}""");
        var commands = new FakePackageReferenceCommands(() =>
            Head(_copyFolder) == Head(_remote) && Head(extCopyFolder) == Head(extRemote));

        // Act
        bool result = await CreateSut(commands).DistributePackagesToProject("App", ["Lib", "Ext"]);

        // Assert
        Assert.True(result);
        string libReference = Path.Combine(_copyFolder, "Lib.A", "Lib.A.csproj");
        string extReference = Path.Combine(extCopyFolder, "Ext.A", "Ext.A.csproj");
        Assert.Equal([
            $"remove reference {projectFile} {libReference}", $"add package {projectFile} Lib.A 1.0.3",
            $"restore {projectFile}", $"remove reference {projectFile} {extReference}",
            $"add package {projectFile} Ext.A 1.0.5", $"restore {projectFile}"
        ], commands.Calls);
        Assert.Equal([true, true], commands.CopiesWereUpdatedAtRestore);
        string packagesProps = ReadPackagesProps();
        Assert.Contains("""<PackageVersion Include="Lib.B" Version="1.0.2" />""", packagesProps,
            StringComparison.Ordinal);
        Assert.Contains("""<PackageVersion Include="Ext.B" Version="1.0.4" />""", packagesProps,
            StringComparison.Ordinal);
    }

    //every copy is pulled before anything changes, so a copy that cannot be fast-forwarded leaves App as it was, even
    //when the copy of an earlier package was pulled already
    [Fact]
    public async Task DistributePackagesToProject_WhenACopyCannotBeFastForwarded_ChangesNothing()
    {
        // Arrange
        AddPackageGitBehindItsRemote("Ext");
        string extCopyFolder = Path.Combine(_appFolder, "Ext");
        await File.WriteAllTextAsync(Path.Combine(extCopyFolder, "Local.txt"), "local");
        GitTestRepositories.RunGit("-C", extCopyFolder, "add", "--all");
        GitTestRepositories.RunGit("-C", extCopyFolder, "commit", "-m", "Local");
        await File.WriteAllTextAsync(Path.Combine(_mainRepoFolder, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Lib\Lib.A\Lib.A.csproj" />
              </ItemGroup>
            </Project>
            """);
        var commands = new FakePackageReferenceCommands(() => true);

        // Act
        bool result = await CreateSut(commands).DistributePackagesToProject("App", ["Lib", "Ext"]);

        // Assert
        Assert.False(result);
        Assert.Contains($"Cannot pull package repository {extCopyFolder} with --ff-only for project App, skipped",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(commands.Calls);
        Assert.Empty(_server.Requests);
        Assert.Equal(PackagesProps, ReadPackagesProps());
    }

    //GitProjects has no record of the project's SpaProjectName (Update Git Projects has not run): only the front git
    //needs its folder, and the distribution touches only the main and the package gits
    [Fact]
    public async Task DistributePackagesToProject_WhenTheSpaProjectRecordIsMissing_DistributesToTheMainRepository()
    {
        // Arrange
        AddFrontGitWithMissingSpaProject();

        // Act
        bool result = await CreateSut().DistributePackagesToProject("App", ["Lib"]);

        // Assert
        Assert.True(result);
        Assert.Equal(Head(_remote), Head(_copyFolder));
        Assert.Contains("""<PackageVersion Include="Lib.B" Version="1.0.2" />""", ReadPackagesProps(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("[ERROR]", ConsoleText(), StringComparison.Ordinal);
    }

    //a git that the distribution needs lies in the SPA project's folder, which cannot be found: the project fails with
    //a message that names the missing record, and nothing changes
    [Fact]
    public async Task DistributePackagesToProject_WhenAUsedGitNeedsTheMissingSpaProject_FailsAndChangesNothing()
    {
        // Arrange
        AddFrontGitWithMissingSpaProject();
        _parameters.Gits["Lib"].GitProjectFolderName = $@"{GitDataModel.SpaProjectFolderRelativePathName}\Lib";

        // Act
        bool result = await CreateSut().DistributePackagesToProject("App", ["Lib"]);

        // Assert
        Assert.False(result);
        Assert.Contains(
            "GitProjects has no AppFront (SpaProjectName of project App): run \"Update Git Projects\" or \"Update Git Projects From SupportToolsServer\"",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(_initialCommit, Head(_copyFolder));
        Assert.Equal(PackagesProps, ReadPackagesProps());
    }

    [Fact]
    public async Task DistributePackagesToProject_WhenTheProjectIsNotRegistered_FailsAndPullsNothing()
    {
        // Act
        bool result = await CreateSut().DistributePackagesToProject("Missing", ["Lib"]);

        // Assert
        Assert.False(result);
        Assert.Contains("Project Missing does not found", ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(_initialCommit, Head(_copyFolder));
    }

    private PackageDistributor CreateSut()
    {
        return new PackageDistributor(new Mock<ILogger>().Object, _httpClientFactory.Object, _parametersManager.Object);
    }

    private PackageDistributor CreateSut(IPackageReferenceCommands commands)
    {
        return new PackageDistributor(new Mock<ILogger>().Object, _httpClientFactory.Object, _parametersManager.Object,
            commands);
    }

    //package project {name}: its git has project {name}.A and is cloned next to App. Another computer then pushed
    //project {name}.B, so the copy is behind its remote. Returns the bare remote
    private string AddPackageGitBehindItsRemote(string name)
    {
        string remote = GitTestRepositories.CreateRemote(_tempFolder, name,
            new Dictionary<string, string>
            {
                [".gitignore"] = "bin/", [$@"{name}.A\{name}.A.csproj"] = """<Project Sdk="Microsoft.NET.Sdk" />"""
            });
        string copyFolder = Path.Combine(_appFolder, name);
        GitTestRepositories.RunGit("clone", remote, copyFolder);
        GitTestRepositories.ConfigureRepository(copyFolder);

        string otherComputerCopy = Path.Combine(_tempFolder, "OtherComputer", name);
        GitTestRepositories.RunGit("clone", remote, otherComputerCopy);
        GitTestRepositories.ConfigureRepository(otherComputerCopy);
        Directory.CreateDirectory(Path.Combine(otherComputerCopy, $"{name}.B"));
        File.WriteAllText(Path.Combine(otherComputerCopy, $"{name}.B", $"{name}.B.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk" />""");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "add", "--all");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "commit", "-m", $"{name}.B");
        GitTestRepositories.RunGit("-C", otherComputerCopy, "push");

        _parameters.Gits[name] = new GitDataModel
        {
            GitProjectAddress = remote, GitProjectFolderName = name, GitIgnorePatternName = "CSharp"
        };
        _parameters.Projects[name] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, UseLocalPackages = true, GitProjectNames = [name]
        };
        _parameters.Projects["App"].GitProjectNames.Add(name);
        return remote;
    }

    //App gets SpaProjectName AppFront without a GitProjects record and a front git in the SPA project's folder
    private void AddFrontGitWithMissingSpaProject()
    {
        _parameters.Projects["App"].SpaProjectName = "AppFront";
        _parameters.Gits["AppFront"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/AppFront.git",
            GitProjectFolderName = $@"{GitDataModel.SpaProjectFolderRelativePathName}\AppFront",
            GitIgnorePatternName = "Node"
        };
        _parameters.Projects["App"].GitProjectNames.Add("AppFront");
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

    //records the csproj changes instead of running dotnet; at every restore it notes whether the copies are updated
    private sealed class FakePackageReferenceCommands : IPackageReferenceCommands
    {
        private readonly Func<bool> _copiesAreUpdated;

        // ReSharper disable once ConvertToPrimaryConstructor
        public FakePackageReferenceCommands(Func<bool> copiesAreUpdated)
        {
            _copiesAreUpdated = copiesAreUpdated;
        }

        public List<string> Calls { get; } = [];
        public List<bool> CopiesWereUpdatedAtRestore { get; } = [];

        public Result RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath)
        {
            Calls.Add($"remove reference {projectFilePath} {referenceProjectFilePath}");
            return Result.Success();
        }

        public Result AddPackageToProjectWithoutRestore(string projectFilePath, string packageName,
            string packageVersion)
        {
            Calls.Add($"add package {projectFilePath} {packageName} {packageVersion}");
            return Result.Success();
        }

        public Result RestoreWithoutHttpCache(string projectFilePath)
        {
            Calls.Add($"restore {projectFilePath}");
            CopiesWereUpdatedAtRestore.Add(_copiesAreUpdated());
            return Result.Success();
        }

        public Result RemovePackageFromProject(string projectFilePath, string packageName)
        {
            Calls.Add($"remove package {projectFilePath} {packageName}");
            return Result.Success();
        }

        public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
        {
            Calls.Add($"add reference {projectFilePath} {referenceProjectFilePath}");
            return Result.Success();
        }
    }
}
