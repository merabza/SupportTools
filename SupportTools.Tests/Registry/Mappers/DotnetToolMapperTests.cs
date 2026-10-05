using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class DotnetToolMapperTests
{
    //InstalledVersion, LatestVersion and CommandName belong to the computer: the round trip restores the shared fields
    //onto the local record, which keeps its own machine fields
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal()
    {
        // Arrange
        DotnetToolData original = NewDotnetTool();
        StsDotnetToolDataModel contract = DotnetToolMapper.ToContract("dotnet-ef", original);
        var restored = new DotnetToolData
        {
            InstalledVersion = original.InstalledVersion,
            LatestVersion = original.LatestVersion,
            CommandName = original.CommandName
        };

        // Act
        DotnetToolMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ToContract_WhenCalled_LeavesMachineFieldsOut()
    {
        // Act
        StsDotnetToolDataModel result = DotnetToolMapper.ToContract("dotnet-ef", NewDotnetTool());

        // Assert
        Assert.Equal("dotnet-ef", result.Name);
        Assert.Equal("dotnet-ef", result.PackageId);
        Assert.Equal("9.0.0", result.MaxVersion);
        Assert.Equal("EF Core tools", result.Description);
        Assert.DoesNotContain("8.0.1", MapperTestHelpers.JsonOf(result));
    }

    //the package id is required on the server, which refuses an empty one
    [Fact]
    public void ToContract_WhenPackageIdIsMissing_SendsEmptyText()
    {
        // Act
        StsDotnetToolDataModel result = DotnetToolMapper.ToContract("dotnet-ef", new DotnetToolData());

        // Assert
        Assert.Equal(string.Empty, result.PackageId);
    }

    [Fact]
    public void ApplyToLocal_WhenServerChangedSharedFields_KeepsMachineFields()
    {
        // Arrange
        DotnetToolData local = NewDotnetTool();
        var contract = new StsDotnetToolDataModel
        {
            Name = "dotnet-ef", PackageId = "dotnet-ef", MaxVersion = null, Description = "changed"
        };

        // Act
        DotnetToolMapper.ApplyToLocal(contract, local);

        // Assert
        Assert.Null(local.MaxVersion);
        Assert.Equal("changed", local.Description);
        Assert.Equal("8.0.1", local.InstalledVersion);
        Assert.Equal("10.0.0", local.LatestVersion);
        Assert.Equal("dotnet ef", local.CommandName);
    }

    [Fact]
    public void Normalize_WhenOptionalFieldsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsDotnetToolDataModel { Name = "t", PackageId = "t", MaxVersion = "", Description = "" };
        var missing = new StsDotnetToolDataModel { Name = "t", PackageId = "t" };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(DotnetToolMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(DotnetToolMapper.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    private static DotnetToolData NewDotnetTool()
    {
        return new DotnetToolData
        {
            PackageId = "dotnet-ef",
            InstalledVersion = "8.0.1",
            LatestVersion = "10.0.0",
            MaxVersion = "9.0.0",
            CommandName = "dotnet ef",
            Description = "EF Core tools"
        };
    }
}
