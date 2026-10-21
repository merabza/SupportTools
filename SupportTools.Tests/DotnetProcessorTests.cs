using LibDotnetWork;
using Xunit;

namespace SupportTools.Tests;

//Distribute all packages builds a project right after it uploads the packages it depends on, so its restore must skip
//the NuGet HTTP cache; every other build keeps the cache
public sealed class DotnetProcessorTests
{
    [Theory]
    [InlineData(false, false,
        "build App.slnx \"-flp:LogFile=C:\\Temp\\LastBuild.log;Verbosity=quiet;Summary\"")]
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

    [Fact]
    public void CreateRestoreWithoutHttpCacheArguments_SkipsTheHttpCache()
    {
        // Act
        string result = DotnetProcessor.CreateRestoreWithoutHttpCacheArguments("App.csproj");

        // Assert
        Assert.Equal("restore App.csproj --no-http-cache", result);
    }
}
