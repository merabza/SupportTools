using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ServerInfoMapperTests
{
    private const string GuidKey = "6a7b0000-fake-guid";

    [Fact]
    public void ToContract_WhenCalled_CopiesEveryField()
    {
        // Arrange
        ServerInfoModel serverInfo = ProjectTestData.NewProject().ServerInfos[ProjectTestData.ProdKey];

        // Act
        StsServerInfoDataModel result = ServerInfoMapper.ToContract(serverInfo, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(("PAZISI", "Production", "Pc1.WebAgent", 5050),
            (result.ServerName, result.EnvironmentName, result.WebAgentNameForCheck, result.ServerSidePort));
        Assert.Equal(("v1", "appfake"), (result.ApiVersionId, result.ServiceUserName));
        Assert.Equal(@"D:\1WorkDotnet\Security\AppFake\appsettings.Production.json",
            result.AppSettingsJsonSourceFileName);
        Assert.Equal(@"D:\1WorkDotnet\AppFake\Encoded\appsettings.Production.json",
            result.AppSettingsEncodedJsonFileName);
        Assert.Equal(["ProgramUpdater", "AppSettingsUpdater"], result.AllowToolsList);
        Assert.Equal(("AppFake", "AppFakeNew"),
            (result.CurrentDatabaseParameters?.DatabaseName, result.NewDatabaseParameters?.DatabaseName));
    }

    //the contract's names are required, a local record without them gives empty names and no tools
    [Fact]
    public void ToContract_WhenNamesAndToolsAreMissing_GivesEmptyNamesAndNoTools()
    {
        // Act
        StsServerInfoDataModel result =
            ServerInfoMapper.ToContract(new ServerInfoModel(), MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal((string.Empty, string.Empty), (result.ServerName, result.EnvironmentName));
        Assert.Empty(result.AllowToolsList);
        Assert.Null(result.CurrentDatabaseParameters);
        Assert.Null(result.NewDatabaseParameters);
    }

    [Fact]
    public void ToContract_WhenComputerIsLinux_GivesCanonicalAppSettingsPaths()
    {
        // Arrange
        var serverInfo = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = "/home/u/1WorkDotnet/Security/appsettings.json",
            AppSettingsEncodedJsonFileName = "/home/u/1WorkDotnet/Encoded/appsettings.json"
        };

        // Act
        StsServerInfoDataModel result = ServerInfoMapper.ToContract(serverInfo, MapperTestHelpers.LinuxPathMapper());

        // Assert
        Assert.Equal(@"D:\1WorkDotnet\Security\appsettings.json", result.AppSettingsJsonSourceFileName);
        Assert.Equal(@"D:\1WorkDotnet\Encoded\appsettings.json", result.AppSettingsEncodedJsonFileName);
    }

    [Theory]
    [InlineData("PAZISI", "Production", "PAZISI|Production")]
    [InlineData(null, "Production", "|Production")]
    [InlineData("PAZISI", null, "PAZISI|")]
    public void NaturalKey_WhenCalled_GivesTheGetItemKeyForm(string? serverName, string? environmentName,
        string expected)
    {
        // Act
        string result = ServerInfoMapper.NaturalKey(serverName, environmentName);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expected,
            new ServerInfoModel { ServerName = serverName, EnvironmentName = environmentName }.GetItemKey());
    }

    [Fact]
    public void ApplyToLocal_WhenContractsComeFromLocalRecords_RestoresThem()
    {
        // Arrange
        Dictionary<string, ServerInfoModel> original = ProjectTestData.NewProject().ServerInfos;
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        List<StsServerInfoDataModel> contracts =
            [.. original.Values.Select(x => ServerInfoMapper.ToContract(x, pathMapper))];
        Dictionary<string, ServerInfoModel> restored = [];

        // Act
        ServerInfoMapper.ApplyToLocal(contracts, restored, pathMapper);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ApplyToLocal_WhenComputerIsLinux_GivesLocalAppSettingsPaths()
    {
        // Arrange
        Dictionary<string, ServerInfoModel> serverInfos = [];
        StsServerInfoDataModel contract = NewContract("PAZISI", "Production");
        contract.AppSettingsJsonSourceFileName = @"D:\1WorkDotnet\Security\appsettings.json";
        contract.AppSettingsEncodedJsonFileName = @"D:\1WorkDotnet\Encoded\appsettings.json";

        // Act
        ServerInfoMapper.ApplyToLocal([contract], serverInfos, MapperTestHelpers.LinuxPathMapper());

        // Assert
        ServerInfoModel result = serverInfos["PAZISI|Production"];
        Assert.Equal("/home/u/1WorkDotnet/Security/appsettings.json", result.AppSettingsJsonSourceFileName);
        Assert.Equal("/home/u/1WorkDotnet/Encoded/appsettings.json", result.AppSettingsEncodedJsonFileName);
    }

    //the set is replaced as a whole inside the same dictionary: a kept record keeps its key and object, a new one gets
    //"Server|Env", a record that the server does not have is removed
    [Fact]
    public void ApplyToLocal_WhenRecordsExist_KeepsKeysAddsNewOnesAndRemovesTheRest()
    {
        // Arrange
        var kept = new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        Dictionary<string, ServerInfoModel> serverInfos = new()
        {
            [GuidKey] = kept, ["Old|Gone"] = new ServerInfoModel { ServerName = "Old", EnvironmentName = "Gone" }
        };
        StsServerInfoDataModel keptContract = NewContract("pazisi", "PRODUCTION");
        keptContract.ServerSidePort = 7070;

        // Act
        ServerInfoMapper.ApplyToLocal([NewContract("Merinson", "Test"), keptContract], serverInfos,
            MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(["Merinson|Test", GuidKey], serverInfos.Keys);
        Assert.Same(kept, serverInfos[GuidKey]);
        Assert.Equal(("pazisi", "PRODUCTION", 7070), (kept.ServerName, kept.EnvironmentName, kept.ServerSidePort));
    }

    //a local error: two records of one server and environment; the first keeps its key, the second is removed
    [Fact]
    public void ApplyToLocal_WhenTwoLocalRecordsHaveTheSameNaturalKey_KeepsTheFirst()
    {
        // Arrange
        var first = new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        Dictionary<string, ServerInfoModel> serverInfos = new()
        {
            [GuidKey] = first,
            ["PAZISI|Production"] = new ServerInfoModel { ServerName = "pazisi", EnvironmentName = "production" }
        };

        // Act
        ServerInfoMapper.ApplyToLocal([NewContract("PAZISI", "Production")], serverInfos,
            MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Same(first, Assert.Single(serverInfos).Value);
        Assert.Equal(GuidKey, serverInfos.Keys.Single());
    }

    //a kept record with other names can hold the "Server|Env" key of a new one: the new one gets a GUID
    [Fact]
    public void ApplyToLocal_WhenTheServerEnvKeyIsTakenByAKeptRecord_GivesTheNewRecordAGuidKey()
    {
        // Arrange
        var kept = new ServerInfoModel { ServerName = "Merinson", EnvironmentName = "Test" };
        Dictionary<string, ServerInfoModel> serverInfos = new() { ["PAZISI|Production"] = kept };

        // Act
        ServerInfoMapper.ApplyToLocal([NewContract("PAZISI", "Production"), NewContract("Merinson", "Test")],
            serverInfos, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Same(kept, serverInfos["PAZISI|Production"]);
        string newKey = Assert.Single(serverInfos.Keys, x => x != "PAZISI|Production");
        Assert.True(Guid.TryParse(newKey, out _));
        Assert.Equal("PAZISI", serverInfos[newKey].ServerName);
    }

    [Fact]
    public void ApplyToLocal_WhenRecordExistsAndOnlyValuesChange_UpdatesTheSameObjectAndItsParts()
    {
        // Arrange
        ServerInfoModel existing = ProjectTestData.NewProject().ServerInfos[ProjectTestData.ProdKey];
        List<EProjectServerTools>? tools = existing.AllowToolsList;
        DatabaseParameters? current = existing.CurrentDatabaseParameters;
        DatabaseParameters? newParameters = existing.NewDatabaseParameters;
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };
        StsServerInfoDataModel contract = ServerInfoMapper.ToContract(existing, MapperTestHelpers.WindowsPathMapper());
        contract.AllowToolsList = ["serviceStarter"];
        contract.CurrentDatabaseParameters!.DatabaseName = "ChangedCurrent";
        contract.NewDatabaseParameters!.DatabaseName = "ChangedNew";

        // Act
        ServerInfoMapper.ApplyToLocal([contract], serverInfos, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Same(existing, serverInfos[GuidKey]);
        Assert.Same(tools, existing.AllowToolsList);
        Assert.Same(current, existing.CurrentDatabaseParameters);
        Assert.Same(newParameters, existing.NewDatabaseParameters);
        Assert.Equal([EProjectServerTools.ServiceStarter], tools);
        Assert.Equal(("ChangedCurrent", "ChangedNew"), (current?.DatabaseName, newParameters?.DatabaseName));
    }

    //CurrentDatabaseParameters and NewDatabaseParameters are init-only: when one of them appears or disappears the
    //record is replaced under its key; the new object takes over the tools and the other parameters
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ApplyToLocal_WhenDatabaseParametersAppearOrDisappear_ReplacesTheRecordUnderTheSameKey(
        bool currentChanges, bool newChanges)
    {
        // Arrange
        ServerInfoModel existing = ProjectTestData.NewProject().ServerInfos[ProjectTestData.ProdKey];
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };
        StsServerInfoDataModel contract = ServerInfoMapper.ToContract(existing, MapperTestHelpers.WindowsPathMapper());
        if (currentChanges)
        {
            contract.CurrentDatabaseParameters = null;
        }

        if (newChanges)
        {
            contract.NewDatabaseParameters = null;
        }

        // Act
        ServerInfoMapper.ApplyToLocal([contract], serverInfos, MapperTestHelpers.WindowsPathMapper());

        // Assert
        ServerInfoModel replaced = serverInfos[GuidKey];
        Assert.NotSame(existing, replaced);
        Assert.Same(existing.AllowToolsList, replaced.AllowToolsList);
        Assert.Same(currentChanges ? null : existing.CurrentDatabaseParameters, replaced.CurrentDatabaseParameters);
        Assert.Same(newChanges ? null : existing.NewDatabaseParameters, replaced.NewDatabaseParameters);
        Assert.Equal(("PAZISI", 5050), (replaced.ServerName, replaced.ServerSidePort));
    }

    [Fact]
    public void ApplyToLocal_WhenDatabaseParametersAppear_CreatesThem()
    {
        // Arrange
        var existing = new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };
        StsServerInfoDataModel contract = NewContract("PAZISI", "Production");
        contract.CurrentDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseName = "Current" };
        contract.NewDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseName = "New" };

        // Act
        ServerInfoMapper.ApplyToLocal([contract], serverInfos, MapperTestHelpers.WindowsPathMapper());

        // Assert
        ServerInfoModel replaced = serverInfos[GuidKey];
        Assert.Equal(("Current", "New"),
            (replaced.CurrentDatabaseParameters?.DatabaseName, replaced.NewDatabaseParameters?.DatabaseName));
    }

    //an empty list and no list are the same: a missing local list is not created for an empty contract list
    [Fact]
    public void ApplyToLocal_WhenLocalToolsAreMissingAndContractHasNone_KeepsThemMissing()
    {
        // Arrange
        var existing = new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };

        // Act
        ServerInfoMapper.ApplyToLocal([NewContract("PAZISI", "Production")], serverInfos,
            MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Null(existing.AllowToolsList);
    }

    [Fact]
    public void ApplyToLocal_WhenLocalToolsAreMissingAndContractHasSome_CreatesTheList()
    {
        // Arrange
        var existing = new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };
        StsServerInfoDataModel contract = NewContract("PAZISI", "Production");
        contract.AllowToolsList = ["VersionChecker", "progpublisher"];

        // Act
        ServerInfoMapper.ApplyToLocal([contract], serverInfos, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal([EProjectServerTools.VersionChecker, EProjectServerTools.ProgPublisher], existing.AllowToolsList);
    }

    [Fact]
    public void ApplyToLocal_WhenLocalToolsExistAndContractHasNone_ClearsTheSameList()
    {
        // Arrange
        List<EProjectServerTools> tools = [EProjectServerTools.ProgramUpdater];
        var existing = new ServerInfoModel
        {
            ServerName = "PAZISI", EnvironmentName = "Production", AllowToolsList = tools
        };
        Dictionary<string, ServerInfoModel> serverInfos = new() { [GuidKey] = existing };

        // Act
        ServerInfoMapper.ApplyToLocal([NewContract("PAZISI", "Production")], serverInfos,
            MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Same(tools, existing.AllowToolsList);
        Assert.Empty(tools);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmpty_GivesNullsAndKeepsTheRequiredNames()
    {
        // Arrange
        StsServerInfoDataModel empty = NewContract("PAZISI", "Production");
        empty.WebAgentNameForCheck = "";
        empty.ApiVersionId = "";
        empty.AppSettingsJsonSourceFileName = "";
        empty.AppSettingsEncodedJsonFileName = "";
        empty.ServiceUserName = "";

        // Act
        StsServerInfoDataModel result = ServerInfoMapper.Normalize(empty);

        // Assert
        Assert.Equal(MapperTestHelpers.HashOf(NewContract("PAZISI", "Production")), MapperTestHelpers.HashOf(result));
        Assert.Equal(("PAZISI", "Production"), (result.ServerName, result.EnvironmentName));
    }

    [Fact]
    public void Normalize_WhenCalled_SortsToolsWithCanonicalNamesAndUpperCasesTheDrives()
    {
        // Arrange
        StsServerInfoDataModel contract = NewContract("PAZISI", "Production");
        contract.AllowToolsList = ["versionchecker", "AppSettingsEncoder", "progpublisher"];
        contract.AppSettingsJsonSourceFileName = @"d:\1WorkDotnet\Security\appsettings.json";
        contract.AppSettingsEncodedJsonFileName = @"d:\1WorkDotnet\Encoded\appsettings.json";
        contract.CurrentDatabaseParameters = new StsDatabaseParametersDataModel { BackupType = "full", DateMask = "" };
        contract.NewDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseRecoveryModel = "simple" };

        // Act
        StsServerInfoDataModel result = ServerInfoMapper.Normalize(contract);

        // Assert
        Assert.Equal(["AppSettingsEncoder", "ProgPublisher", "VersionChecker"], result.AllowToolsList);
        Assert.Equal(@"D:\1WorkDotnet\Security\appsettings.json", result.AppSettingsJsonSourceFileName);
        Assert.Equal(@"D:\1WorkDotnet\Encoded\appsettings.json", result.AppSettingsEncodedJsonFileName);
        Assert.Equal(("Full", (string?)null),
            (result.CurrentDatabaseParameters?.BackupType, result.CurrentDatabaseParameters?.DateMask));
        Assert.Equal("Simple", result.NewDatabaseParameters?.DatabaseRecoveryModel);
    }

    [Fact]
    public void FindUnknownEnumField_WhenEveryNameIsKnown_ReturnsNull()
    {
        // Arrange
        StsServerInfoDataModel contract = ServerInfoMapper.ToContract(
            ProjectTestData.NewProject().ServerInfos[ProjectTestData.ProdKey], MapperTestHelpers.WindowsPathMapper());

        // Act
        string? result = ServerInfoMapper.FindUnknownEnumField(contract);

        // Assert
        Assert.Null(result);
    }

    //the first unknown name in the order of the contract: the tools, then the current and the new parameters
    [Theory]
    [InlineData(true, false, false, "PAZISI|Production.AllowToolsList")]
    [InlineData(true, true, true, "PAZISI|Production.AllowToolsList")]
    [InlineData(false, true, false, "PAZISI|Production.CurrentDatabaseParameters.BackupType")]
    [InlineData(false, true, true, "PAZISI|Production.CurrentDatabaseParameters.BackupType")]
    [InlineData(false, false, true, "PAZISI|Production.NewDatabaseParameters.DatabaseRecoveryModel")]
    public void FindUnknownEnumField_WhenANameIsUnknown_ReturnsItsField(bool unknownTool, bool unknownCurrent,
        bool unknownNew, string expected)
    {
        // Arrange
        StsServerInfoDataModel contract = ServerInfoMapper.ToContract(
            ProjectTestData.NewProject().ServerInfos[ProjectTestData.ProdKey], MapperTestHelpers.WindowsPathMapper());
        if (unknownTool)
        {
            contract.AllowToolsList.Add("FutureServerTool");
        }

        if (unknownCurrent)
        {
            contract.CurrentDatabaseParameters!.BackupType = "Snapshot";
        }

        if (unknownNew)
        {
            contract.NewDatabaseParameters!.DatabaseRecoveryModel = "Instant";
        }

        // Act
        string? result = ServerInfoMapper.FindUnknownEnumField(contract);

        // Assert
        Assert.Equal(expected, result);
    }

    private static StsServerInfoDataModel NewContract(string serverName, string environmentName)
    {
        return new StsServerInfoDataModel { ServerName = serverName, EnvironmentName = environmentName };
    }
}
