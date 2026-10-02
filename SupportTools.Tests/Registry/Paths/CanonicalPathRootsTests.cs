using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Paths;

public sealed class CanonicalPathRootsTests
{
    [Theory]
    [InlineData(@"D:\1WorkDotnet\X\Y.slnx", @"D:\1WorkDotnet")]
    [InlineData(@"d:\1WorkPackages\AppCliTools", @"D:\1WorkPackages")]
    [InlineData(@"D:\1WorkLTG", @"D:\1WorkLTG")]
    [InlineData(@"D:\1WorkDotnet\VerbsGrammarGe\", @"D:\1WorkDotnet")]
    [InlineData(@"D:\\WebAgentData\\ProdBaseBackups", @"D:\WebAgentData")]
    [InlineData("D:/1WorkDotnet/X", @"D:\1WorkDotnet")]
    [InlineData(@"E:\", @"E:\")]
    public void GetRoot_WhenPathIsWindowsRooted_ReturnsDriveAndFirstFolder(string path, string expected)
    {
        // Act
        string? result = CanonicalPathRoots.GetRoot(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("D:")]
    [InlineData("D:x")]
    [InlineData("ftp://192.168.10.50/ProdBaseExchange/")]
    [InlineData("/home/u/1WorkDotnet")]
    [InlineData(@"src\appcarcass")]
    [InlineData(@"\\server\share\x")]
    public void GetRoot_WhenPathIsNotDriveRooted_ReturnsNull(string? path)
    {
        // Act
        string? result = CanonicalPathRoots.GetRoot(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Collect_WhenParametersHaveNoPaths_ReturnsEmptyList()
    {
        // Act
        List<string> result = CanonicalPathRoots.Collect(new SupportToolsParameters());

        // Assert
        Assert.Empty(result);
    }

    //every path field of a project listed in README §4.4 gives its own root here
    [Fact]
    public void Collect_WhenProjectHasPathFields_ReturnsRootOfEveryField()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            Projects =
            {
                ["Project"] = new ProjectModel
                {
                    ProjectFolderName = @"C:\R01\x",
                    SolutionFileName = @"C:\R02\x.slnx",
                    ProjectSecurityFolderPath = @"C:\R03\x",
                    MigrationStartupProjectFilePath = @"C:\R04\x.csproj",
                    MigrationProjectFilePath = @"C:\R05\x.csproj",
                    SeedProjectFilePath = @"C:\R06\x.csproj",
                    SeedProjectParametersFilePath = @"C:\R07\x.json",
                    DataSeederRulesByTableStartupProjectFilePath = @"C:\R08\x.csproj",
                    OldDataConvertorForDataSeeder = @"C:\R09\x.csproj",
                    ExcludesRulesParametersFilePath = @"C:\R10\x.json",
                    AppSetEnKeysJsonFileName = @"C:\R11\x.json",
                    MigrationSqlFilesFolder = @"C:\R12\x",
                    PrepareProdCopyDatabaseProjectFilePath = @"C:\R13\x.csproj",
                    PrepareProdCopyDatabaseProjectParametersFilePath = @"C:\R14\x.json",
                    PairedDbObjectsResultFileName = @"C:\R15\x.json"
                }
            }
        };
        string[] expected =
        [
            @"C:\R01", @"C:\R02", @"C:\R03", @"C:\R04", @"C:\R05", @"C:\R06", @"C:\R07", @"C:\R08", @"C:\R09",
            @"C:\R10", @"C:\R11", @"C:\R12", @"C:\R13", @"C:\R14", @"C:\R15"
        ];

        // Act
        List<string> result = CanonicalPathRoots.Collect(parameters);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Collect_WhenServerInfosProjectCreatorAndFileStoragesHavePaths_ReturnsTheirRoots()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            Projects =
            {
                ["Project"] = new ProjectModel
                {
                    ServerInfos =
                    {
                        ["Server|Prod"] = new ServerInfoModel
                        {
                            AppSettingsJsonSourceFileName = @"D:\Source\appsettings.json",
                            AppSettingsEncodedJsonFileName = @"D:\Encoded\appsettingsEncoded.json"
                        }
                    }
                }
            },
            AppProjectCreatorAllParameters =
                new AppProjectCreatorAllParameters
                {
                    ProjectsFolderPathReal = @"D:\Projects", SecretsFolderPathReal = @"D:\Secrets"
                },
            FileStorages =
            {
                ["Local"] = new FileStorageData { FileStoragePath = @"E:\BAK\" },
                ["Ftp"] = new FileStorageData { FileStoragePath = "ftp://192.168.10.50/ProdBaseExchange/" },
                ["Empty"] = new FileStorageData()
            }
        };
        string[] expected = [@"D:\Encoded", @"D:\Projects", @"D:\Secrets", @"D:\Source", @"E:\BAK"];

        // Act
        List<string> result = CanonicalPathRoots.Collect(parameters);

        // Assert
        Assert.Equal(expected, result);
    }

    //the same root written with another case is one root; relative paths are skipped
    [Fact]
    public void Collect_WhenRootsDifferOnlyByCase_ReturnsEachRootOnceInOrder()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            Projects =
            {
                ["B"] = new ProjectModel
                {
                    ProjectFolderName = @"d:\1WorkPackages\AppCliTools\",
                    SolutionFileName = @"D:\1WorkPackages\AppCliTools\AppCliTools\AppCliTools.slnx"
                },
                ["A"] = new ProjectModel
                {
                    ProjectFolderName = @"D:\1WorkDotnet\SupportTools",
                    MigrationSqlFilesFolder = @"Sql\Migrations"
                }
            }
        };
        string[] expected = [@"D:\1WorkDotnet", @"D:\1WorkPackages"];

        // Act
        List<string> result = CanonicalPathRoots.Collect(parameters);

        // Assert
        Assert.Equal(expected, result);
    }
}
