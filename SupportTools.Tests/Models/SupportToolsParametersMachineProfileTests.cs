using System;
using System.IO;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Models;

public sealed class SupportToolsParametersMachineProfileTests : IDisposable
{
    private readonly string _parametersFileName;
    private readonly string _rootFolder;

    public SupportToolsParametersMachineProfileTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        _parametersFileName = Path.Combine(_rootFolder, "SupportTools.json");
    }

    public void Dispose()
    {
        Directory.Delete(_rootFolder, true);
    }

    //a parameters file written before the machine profile fields existed must still load
    [Fact]
    public async Task Load_WhenFileHasNoMachineProfileFields_UsesDefaults()
    {
        // Arrange
        await File.WriteAllTextAsync(_parametersFileName,
            """{ "LogFolder": "D:\\Logs", "Servers": { "PAZISI": { "IsLocal": true } } }""");

        // Act
        SupportToolsParameters parameters = Load();

        // Assert
        Assert.Null(parameters.MachineName);
        Assert.Null(parameters.CurrentMachineServerName);
        Assert.Empty(parameters.PathMappings);
        Assert.True(parameters.Servers["PAZISI"].IsLocal);
    }

    [Fact]
    public async Task SaveThenLoad_WhenMachineProfileFieldsAreSet_KeepsThem()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            MachineName = "Merinson",
            CurrentMachineServerName = "Merinson",
            PathMappings =
            {
                new PathMappingModel
                {
                    CanonicalPrefix = @"D:\1WorkDotnet", LocalPrefix = "/home/u/1WorkDotnet"
                },
                new PathMappingModel
                {
                    CanonicalPrefix = @"D:\1WorkSecurity", LocalPrefix = "/home/u/1WorkSecurity"
                }
            }
        };
        var parametersManager = new ParametersManager(null, parameters);

        // Act
        await parametersManager.Save(parameters, null, _parametersFileName);
        SupportToolsParameters loaded = Load();

        // Assert
        Assert.Equal("Merinson", loaded.MachineName);
        Assert.Equal("Merinson", loaded.CurrentMachineServerName);
        Assert.Collection(loaded.PathMappings, x =>
        {
            Assert.Equal(@"D:\1WorkDotnet", x.CanonicalPrefix);
            Assert.Equal("/home/u/1WorkDotnet", x.LocalPrefix);
        }, x =>
        {
            Assert.Equal(@"D:\1WorkSecurity", x.CanonicalPrefix);
            Assert.Equal("/home/u/1WorkSecurity", x.LocalPrefix);
        });
    }

    [Fact]
    public void GetMachineNameOrDefault_WhenMachineNameIsSet_ReturnsIt()
    {
        // Arrange
        var sut = new SupportToolsParameters { MachineName = "Merinson" };

        // Act
        string result = sut.GetMachineNameOrDefault();

        // Assert
        Assert.Equal("Merinson", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMachineNameOrDefault_WhenMachineNameIsNotSet_ReturnsNameOfThisComputer(string? machineName)
    {
        // Arrange
        var sut = new SupportToolsParameters { MachineName = machineName };

        // Act
        string result = sut.GetMachineNameOrDefault();

        // Assert
        Assert.Equal(Environment.MachineName, result);
    }

    private SupportToolsParameters Load()
    {
        var parametersLoader = new ParametersLoader<SupportToolsParameters>();
        Assert.True(parametersLoader.TryLoadParameters(_parametersFileName));
        return Assert.IsType<SupportToolsParameters>(parametersLoader.Par);
    }
}
