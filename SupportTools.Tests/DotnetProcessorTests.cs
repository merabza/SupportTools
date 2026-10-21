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
}
