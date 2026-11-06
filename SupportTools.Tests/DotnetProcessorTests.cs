using System;
using LibDotnetWork;
using Xunit;

namespace SupportTools.Tests;

//Distribute all packages builds a project right after it uploads the packages it depends on, so its restore must skip
//the NuGet HTTP cache; every other build keeps the cache
public sealed class DotnetProcessorTests
{
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

    //-n: dotnet outdated writes the versions into Directory.Packages.props itself, without one dotnet add package (with
    //a restore preview and a restore) per project and package
    [Fact]
    public void CreateUpdateOutdatedPackagesArguments_UpgradesWithoutARestorePerPackage()
    {
        // Act
        string result = DotnetProcessor.CreateUpdateOutdatedPackagesArguments(@"C:\Work\GitA");

        // Assert
        Assert.Equal(@"outdated -r -u -n C:\Work\GitA", result);
    }

    [Fact]
    public void CreateRestoreWithoutHttpCacheArguments_SkipsTheHttpCache()
    {
        // Act
        string result = DotnetProcessor.CreateRestoreWithoutHttpCacheArguments("App.csproj");

        // Assert
        Assert.Equal("restore App.csproj --no-http-cache", result);
    }
}
