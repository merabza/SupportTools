using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests;

//Distribute all packages builds a project right after it uploads the packages it depends on, so its restore must skip
//the NuGet HTTP cache; every other build keeps the cache. The commands run no dotnet: FakeDotnetRunner records them
public sealed class DotnetProcessorTests
{
    private readonly ILogger _logger = new Mock<ILogger>().Object;
    private readonly FakeDotnetRunner _runner = new();

    [Theory]
    [InlineData(false, false, "build App.slnx \"-flp:LogFile=C:\\Temp\\LastBuild.log;Verbosity=quiet;Summary\"")]
    [InlineData(true, false,
        "build App.slnx --no-incremental \"-flp:LogFile=C:\\Temp\\LastBuild.log;Verbosity=quiet;Summary\"")]
    [InlineData(false, true,
        "build App.slnx -p:RestoreNoHttpCache=true \"-flp:LogFile=C:\\Temp\\LastBuild.log;Verbosity=quiet;Summary\"")]
    [InlineData(true, true,
        "build App.slnx --no-incremental -p:RestoreNoHttpCache=true \"-flp:LogFile=C:\\Temp\\LastBuild.log;Verbosity=quiet;Summary\"")]
    public void CreateBuildArguments_AddsTheSwitchesThatAreAskedFor(bool noIncremental, bool noHttpCache,
        string expected)
    {
        // Act
        string result =
            DotnetProcessor.CreateBuildArguments("App.slnx", noIncremental, noHttpCache, @"C:\Temp\LastBuild.log");

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, false, "add App.csproj package Lib.A")]
    [InlineData("1.0.1", false, "add App.csproj package Lib.A --version 1.0.1")]
    [InlineData("1.0.1", true, "add App.csproj package Lib.A --version 1.0.1 --no-restore")]
    public void CreateAddPackageArguments_AddsTheSwitchesThatAreAskedFor(string? packageVersion, bool noRestore,
        string expected)
    {
        // Act
        string result = DotnetProcessor.CreateAddPackageArguments("App.csproj", "Lib.A", packageVersion, noRestore);

        // Assert
        Assert.Equal(expected, result);
    }

    //Version, not AssemblyVersion: it also reaches the restore, so a project built from source wins over an older package
    //of the same name (WebSystemTools refers to SystemTools by packages) and the publish holds the DLLs the app references
    [Fact]
    public void CreatePublishArguments_SetsTheVersionOfEveryProject()
    {
        // Act
        string result =
            DotnetProcessor.CreatePublishArguments("linux-x64", @"C:\Publish\App", "App.csproj", "1.0.9792.31628");

        // Assert
        Assert.Equal(
            @"publish --configuration Release --runtime linux-x64 --self-contained --output C:\Publish\App App.csproj /p:Version=1.0.9792.31628",
            result);
    }

    //the app csproj's PublishSpa target gives SpaMode to npm run build as Vite's --mode, so the front takes .env.{mode}
    [Fact]
    public void CreatePublishArguments_WhenSpaModeIsSet_PassesItAsSpaModeProperty()
    {
        // Act
        string result = DotnetProcessor.CreatePublishArguments("linux-x64", @"C:\Publish\App", "App.csproj",
            "1.0.9792.31628", "staging");

        // Assert
        Assert.EndsWith(" /p:Version=1.0.9792.31628 /p:SpaMode=staging", result, StringComparison.Ordinal);
    }

    //an empty mode is not passed: the csproj's own default (production) applies
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreatePublishArguments_WhenSpaModeIsEmpty_DoesNotPassIt(string? spaMode)
    {
        // Act
        string result = DotnetProcessor.CreatePublishArguments("linux-x64", @"C:\Publish\App", "App.csproj",
            "1.0.9792.31628", spaMode);

        // Assert
        Assert.DoesNotContain("SpaMode", result, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRestoreWithoutHttpCacheArguments_SkipsTheHttpCache()
    {
        // Act
        string result = DotnetProcessor.CreateRestoreWithoutHttpCacheArguments("App.csproj");

        // Assert
        Assert.Equal("restore App.csproj --no-http-cache", result);
    }

    //every command runs dotnet with the console flag and the logger of the processor and StShared's defaults, and
    //returns what the run returned
    [Theory]
    [MemberData(nameof(Commands))]
    public void Command_WhenCalled_RunsDotnetWithItsArguments(string command, Func<DotnetProcessor, Result> run,
        string expectedArguments)
    {
        // Arrange
        _runner.Result = Result.Failure(Error.Failure("Fake", command));

        // Act
        Result result = run(CreateSut());

        // Assert
        Assert.Equal(new FakeDotnetRun(false, _logger, "dotnet", expectedArguments, null, true, Timeout.Infinite),
            Assert.Single(_runner.Runs));
        Assert.Same(_runner.Result, result);
    }

    public static TheoryData<string, Func<DotnetProcessor, Result>, string> Commands =>
        new()
        {
            {
                "publish", x => x.PublishRelease("linux-x64", @"C:\Publish\App", "App.csproj", "1.0.1.2", "staging"),
                @"publish --configuration Release --runtime linux-x64 --self-contained --output C:\Publish\App App.csproj /p:Version=1.0.1.2 /p:SpaMode=staging"
            },
            {
                "new solution", x => x.CreateNewSolution(@"C:\Work\App", "App"),
                @"new sln --output C:\Work\App --name App"
            },
            //dotnet new needs the template name in lower case
            {
                "new project", x => x.CreateNewProject(EDotnetProjectType.ClassLib, null, @"C:\Work\App\Lib", "Lib"),
                @"new classlib --output C:\Work\App\Lib --name Lib"
            },
            {
                "new project with blank parameters",
                x => x.CreateNewProject(EDotnetProjectType.Console, " ", @"C:\Work\App\Cli", "Cli"),
                @"new console --output C:\Work\App\Cli --name Cli"
            },
            {
                "new project with parameters",
                x => x.CreateNewProject(EDotnetProjectType.Web, "--no-https", @"C:\Work\App\Api", "Api"),
                @"new web --no-https --output C:\Work\App\Api --name Api"
            },
            {
                "add to solution", x => x.AddProjectToSolution(@"C:\Work\App\App.slnx", null, @"Lib\Lib.csproj"),
                @"sln C:\Work\App\App.slnx add Lib\Lib.csproj"
            },
            {
                "add to solution folder",
                x => x.AddProjectToSolution(@"C:\Work\App\App.slnx", "Libs", @"Lib\Lib.csproj"),
                @"sln C:\Work\App\App.slnx add --solution-folder Libs Lib\Lib.csproj"
            },
            {
                "add reference", x => x.AddReferenceToProject("App.csproj", @"..\Lib\Lib.csproj"),
                @"add App.csproj reference ..\Lib\Lib.csproj"
            },
            {
                "add package", x => x.AddPackageToProject("App.csproj", "Lib.A", "1.0.1"),
                "add App.csproj package Lib.A --version 1.0.1"
            },
            {
                "add package without restore", x => x.AddPackageToProjectWithoutRestore("App.csproj", "Lib.A", "1.0.1"),
                "add App.csproj package Lib.A --version 1.0.1 --no-restore"
            },
            {
                "remove reference", x => x.RemoveReferenceFromProject("App.csproj", @"..\Lib\Lib.csproj"),
                @"remove App.csproj reference ..\Lib\Lib.csproj"
            },
            {
                "remove package", x => x.RemovePackageFromProject("App.csproj", "Lib.A"),
                "remove App.csproj package Lib.A"
            },
            {
                "user secrets", x => x.InitUserSecrets(@"C:\Work\App\App.csproj"),
                @"user-secrets init --project C:\Work\App\App.csproj"
            },
            {
                "drop database", x => x.EfDropDatabase("AppDbContext", "Start.csproj", "Db.csproj"),
                "ef database drop --force --context AppDbContext --startup-project Start.csproj --project Db.csproj"
            },
            {
                "add migration", x => x.EfAddDatabaseMigration("Initial", "AppDbContext", "Start.csproj", "Db.csproj"),
                "ef migrations add \"Initial\" --context AppDbContext --startup-project Start.csproj --project Db.csproj"
            },
            {
                "update database", x => x.EfUpdateDatabaseByMigration("AppDbContext", "Start.csproj", "Db.csproj"),
                "ef database update --context AppDbContext --startup-project Start.csproj --project Db.csproj"
            },
            { "restore", x => x.Restore("App.csproj"), "restore App.csproj" },
            {
                "restore without cache", x => x.RestoreWithoutHttpCache("App.csproj"),
                "restore App.csproj --no-http-cache"
            },
            {
                "pack", x => x.Pack("Lib.csproj", @"C:\Packages", "1.0.5"),
                @"pack Lib.csproj --configuration Release --output C:\Packages -p:PackageVersion=1.0.5"
            },
            {
                "push", x => x.NugetPush("Lib.1.0.5.nupkg", "https://feed/v3/index.json", null),
                "nuget push Lib.1.0.5.nupkg --source https://feed/v3/index.json --skip-duplicate"
            },
            {
                "push with a blank key", x => x.NugetPush("Lib.1.0.5.nupkg", "https://feed/v3/index.json", " "),
                "nuget push Lib.1.0.5.nupkg --source https://feed/v3/index.json --skip-duplicate"
            },
            {
                "push with a key", x => x.NugetPush("Lib.1.0.5.nupkg", "https://feed/v3/index.json", "key1"),
                "nuget push Lib.1.0.5.nupkg --source https://feed/v3/index.json --skip-duplicate --api-key key1"
            },
            { "install tool", x => x.InstallTool("dotnet-ef"), "tool install --global dotnet-ef" },
            { "install tool with no version", x => x.InstallTool("dotnet-ef", ""), "tool install --global dotnet-ef" },
            {
                "install tool version", x => x.InstallTool("dotnet-ef", "10.0.14"),
                "tool install --global dotnet-ef --version 10.0.14"
            },
            { "update tool", x => x.UpdateTool("dotnet-ef"), "tool update --global dotnet-ef" },
            { "update tool with no version", x => x.UpdateTool("dotnet-ef", ""), "tool update --global dotnet-ef" },
            {
                "update tool version", x => x.UpdateTool("dotnet-ef", "10.0.14"),
                "tool update --global dotnet-ef --version 10.0.14"
            }
        };

    [Fact]
    public void SearchTool_WhenCalled_ReturnsTheOutputOfTheSearch()
    {
        // Act
        Result<(string, int)> result = CreateSut().SearchTool("dotnet-ef");

        // Assert
        Assert.Equal(new FakeDotnetRun(false, _logger, "dotnet", "tool search dotnet-ef --take 1", null, true, null),
            Assert.Single(_runner.Runs));
        Assert.Same(_runner.Output, result);
    }

    [Fact]
    public void GetToolsRawList_WhenTheCommandSucceeds_ReturnsItsLines()
    {
        // Arrange
        _runner.Output =
            Result.Success((string.Join(Environment.NewLine, "Package Id  Version", "dotnet-ef  10.0.14"), 0));

        // Act
        Result<IEnumerable<string>> result = CreateSut().GetToolsRawList();

        // Assert
        Assert.Equal("tool list --global", Assert.Single(_runner.Runs).Arguments);
        Assert.Equal(["Package Id  Version", "dotnet-ef  10.0.14"], result.Value);
    }

    [Fact]
    public void GetToolsRawList_WhenTheCommandFails_ReturnsItsError()
    {
        // Arrange
        Error error = Error.Failure("Fake", "tool list failed");
        _runner.Output = Result.Failure<(string, int)>(error);

        // Act
        Result<IEnumerable<string>> result = CreateSut().GetToolsRawList();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    //the lines up to a line of dashes are the header; empty lines are skipped and the paths are trimmed
    [Fact]
    public void GetSolutionProjectsList_WhenTheCommandSucceeds_ReturnsTheProjectsAfterTheHeader()
    {
        // Arrange
        _runner.Output = Result.Success((string.Join(Environment.NewLine, "Project(s)", "----------", @"App\App.csproj",
            string.Empty, @"  Lib\Lib.csproj  ", string.Empty), 0));

        // Act
        Result<List<string>> result = CreateSut().GetSolutionProjectsList("App.slnx");

        // Assert
        Assert.Equal("sln App.slnx list", Assert.Single(_runner.Runs).Arguments);
        Assert.Equal([@"App\App.csproj", @"Lib\Lib.csproj"], result.Value);
    }

    //only a line of dashes ends the header, not a header line with a dash in it
    [Fact]
    public void GetSolutionProjectsList_WhenAHeaderLineHasADash_KeepsReadingTheHeader()
    {
        // Arrange
        _runner.Output = Result.Success((string.Join(Environment.NewLine, "Project-Paths", "-------------",
            @"App\App.csproj"), 0));

        // Act
        Result<List<string>> result = CreateSut().GetSolutionProjectsList("App.slnx");

        // Assert
        Assert.Equal([@"App\App.csproj"], result.Value);
    }

    [Fact]
    public void GetSolutionProjectsList_WhenThereIsNoLineOfDashes_ReturnsNoProject()
    {
        // Arrange
        _runner.Output = Result.Success((string.Join(Environment.NewLine, "Project(s)", @"App\App.csproj"), 0));

        // Act
        Result<List<string>> result = CreateSut().GetSolutionProjectsList("App.slnx");

        // Assert
        Assert.Empty(result.Value);
    }

    [Fact]
    public void GetSolutionProjectsList_WhenTheCommandFails_ReturnsItsError()
    {
        // Arrange
        Error error = Error.Failure("Fake", "sln list failed");
        _runner.Output = Result.Failure<(string, int)>(error);

        // Act
        Result<List<string>> result = CreateSut().GetSolutionProjectsList("App.slnx");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    //the log goes to the temp folder; a failed build makes no pause, so a loop over many projects goes on
    [Fact]
    public void Build_WhenCalled_WritesTheSummaryToTheTempLogWithoutAPause()
    {
        // Act
        CreateSut().Build("App.slnx", true, false);

        // Assert
        FakeDotnetRun run = Assert.Single(_runner.Runs);
        Assert.Equal(DotnetProcessor.CreateBuildArguments("App.slnx", true, false, BuildLogFile), run.Arguments);
        Assert.False(run.UseErrorLine);
        Assert.Null(run.AllowExitCodes);
    }

    [Fact]
    public void Build_WhenTheLogHasTheSummary_ReturnsItsCounts()
    {
        // Arrange
        _runner.OnRun = _ =>
            File.WriteAllLines(BuildLogFile, ["Build succeeded.", "    2 Warning(s)", "    1 Error(s)"]);

        // Act
        DotnetBuildResult result = CreateSut().Build("App.slnx", false, false);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(2, result.WarningCount);
    }

    //the summary of the last project wins; lines that only look like a count are ignored
    [Fact]
    public void Build_WhenTheLogHasSeveralSummaries_ReturnsTheLastCounts()
    {
        // Arrange
        _runner.OnRun = _ => File.WriteAllLines(BuildLogFile,
        [
            "    5 Warning(s)", "    3 Error(s)", "    12 Warning(s)", "    4 Error(s)", "    x Warning(s)",
            "  -1 Error(s)"
        ]);

        // Act
        DotnetBuildResult result = CreateSut().Build("App.slnx", false, false);

        // Assert
        Assert.Equal(4, result.ErrorCount);
        Assert.Equal(12, result.WarningCount);
    }

    [Fact]
    public void Build_WhenTheCommandFails_ReturnsAFailedBuild()
    {
        // Arrange
        _runner.Result = Result.Failure(Error.Failure("Fake", "build failed"));

        // Act
        DotnetBuildResult result = CreateSut().Build("App.slnx", false, false);

        // Assert
        Assert.False(result.Succeeded);
    }

    //the log of an earlier build is deleted first, so a build that writes no log counts nothing
    [Fact]
    public void Build_WhenNoLogIsWritten_ReturnsNoCountsOfAnEarlierBuild()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(BuildLogFile)!);
        File.WriteAllLines(BuildLogFile, ["    7 Warning(s)", "    4 Error(s)"]);

        // Act
        DotnetBuildResult result = CreateSut().Build("App.slnx", false, false);

        // Assert
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.WarningCount);
        Assert.False(File.Exists(BuildLogFile));
    }

    //a new computer or a cleaned temp folder has no folder for the log yet
    [Fact]
    public void Build_WhenTheLogFolderIsMissing_CreatesIt()
    {
        // Arrange
        string logFolder = Path.GetDirectoryName(BuildLogFile)!;
        if (Directory.Exists(logFolder))
        {
            Directory.Delete(logFolder, true);
        }

        // Act
        CreateSut().Build("App.slnx", false, false);

        // Assert
        Assert.True(Directory.Exists(logFolder));
    }

    private static string BuildLogFile =>
        Path.Combine(Path.GetTempPath(), "SupportTools", "BuildLogs", "LastBuild.log");

    private DotnetProcessor CreateSut()
    {
        return new DotnetProcessor(_logger, false, _runner.RunProcess, _runner.RunProcessWithOutput);
    }
}
