using DatabaseTools.DbTools;
using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class DatabaseParametersMapperTests
{
    [Fact]
    public void ToContract_WhenParametersAreMissing_ReturnsNull()
    {
        // Act
        StsDatabaseParametersDataModel? result = DatabaseParametersMapper.ToContract(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToContract_WhenCalled_CopiesEveryFieldAndGivesEnumNames()
    {
        // Act
        StsDatabaseParametersDataModel? result =
            DatabaseParametersMapper.ToContract(ProjectTestData.NewDatabaseParameters("AppFakeDev"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(("Dev", "Simple", "Default", "AppFakeDev"),
            (result.DbConnectionName, result.DatabaseRecoveryModel, result.DbServerFoldersSetName,
                result.DatabaseName));
        Assert.Equal(("Daily", "Exchange", 600, true),
            (result.SmartSchemaName, result.FileStorageName, result.CommandTimeOut, result.SkipBackupBeforeRestore));
        Assert.Equal(("pre_", "yyyyMMdd", ".bak", "_Full_"),
            (result.BackupNamePrefix, result.DateMask, result.BackupFileExtension, result.BackupNameMiddlePart));
        Assert.Equal((true, false, "Diff"), (result.Compress, result.Verify, result.BackupType));
    }

    [Fact]
    public void ToContract_WhenEnumsAreNotSet_GivesNullNames()
    {
        // Act
        StsDatabaseParametersDataModel? result = DatabaseParametersMapper.ToContract(new DatabaseParameters());

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DatabaseRecoveryModel);
        Assert.Null(result.BackupType);
    }

    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalParameters_RestoresTheOriginal()
    {
        // Arrange
        DatabaseParameters original = ProjectTestData.NewDatabaseParameters("AppFakeDev");
        StsDatabaseParametersDataModel contract = DatabaseParametersMapper.ToContract(original)!;
        var restored = new DatabaseParameters();

        // Act
        DatabaseParametersMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //the server keeps the names as they came, so another case still maps to the enum
    [Fact]
    public void ApplyToLocal_WhenEnumNamesHaveAnotherCase_ParsesThem()
    {
        // Arrange
        var contract =
            new StsDatabaseParametersDataModel { DatabaseRecoveryModel = "bulklogged", BackupType = "trlog" };
        var parameters = new DatabaseParameters();

        // Act
        DatabaseParametersMapper.ApplyToLocal(contract, parameters);

        // Assert
        Assert.Equal(EDatabaseRecoveryModel.BulkLogged, parameters.DatabaseRecoveryModel);
        Assert.Equal(EBackupType.TrLog, parameters.BackupType);
    }

    [Fact]
    public void ApplyToLocal_WhenEnumNamesAreMissing_ClearsTheEnums()
    {
        // Arrange
        DatabaseParameters parameters = ProjectTestData.NewDatabaseParameters("AppFakeDev");

        // Act
        DatabaseParametersMapper.ApplyToLocal(new StsDatabaseParametersDataModel(), parameters);

        // Assert
        Assert.Null(parameters.DatabaseRecoveryModel);
        Assert.Null(parameters.BackupType);
    }

    [Fact]
    public void ToLocal_WhenContractIsMissing_ReturnsNull()
    {
        // Act
        DatabaseParameters? result =
            DatabaseParametersMapper.ToLocal(null, ProjectTestData.NewDatabaseParameters("AppFakeDev"));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToLocal_WhenParametersExist_UpdatesTheSameObject()
    {
        // Arrange
        DatabaseParameters existing = ProjectTestData.NewDatabaseParameters("AppFakeDev");

        // Act
        DatabaseParameters? result = DatabaseParametersMapper.ToLocal(
            new StsDatabaseParametersDataModel { DatabaseName = "Changed", CommandTimeOut = 30 }, existing);

        // Assert
        Assert.Same(existing, result);
        Assert.Equal(("Changed", 30, (string?)null),
            (existing.DatabaseName, existing.CommandTimeOut, existing.DbConnectionName));
    }

    [Fact]
    public void ToLocal_WhenParametersAreNew_CreatesThem()
    {
        // Act
        DatabaseParameters? result = DatabaseParametersMapper.ToLocal(
            new StsDatabaseParametersDataModel { DatabaseName = "New", BackupType = "Full" }, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(("New", EBackupType.Full), (result.DatabaseName, result.BackupType));
    }

    [Fact]
    public void Normalize_WhenContractIsMissing_ReturnsNull()
    {
        // Act
        StsDatabaseParametersDataModel? result = DatabaseParametersMapper.Normalize(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmpty_GivesNulls()
    {
        // Arrange
        var contract = new StsDatabaseParametersDataModel
        {
            DbConnectionName = "",
            DbServerFoldersSetName = "",
            DatabaseName = "",
            SmartSchemaName = "",
            FileStorageName = "",
            BackupNamePrefix = "",
            DateMask = "",
            BackupFileExtension = "",
            BackupNameMiddlePart = ""
        };

        // Act
        StsDatabaseParametersDataModel? result = DatabaseParametersMapper.Normalize(contract);

        // Assert
        Assert.Equal(MapperTestHelpers.HashOf(new StsDatabaseParametersDataModel()), MapperTestHelpers.HashOf(result!));
    }

    [Fact]
    public void Normalize_WhenEnumNamesHaveAnotherCase_GivesTheCanonicalNames()
    {
        // Arrange
        var contract = new StsDatabaseParametersDataModel { DatabaseRecoveryModel = "simple", BackupType = "diff" };

        // Act
        StsDatabaseParametersDataModel? result = DatabaseParametersMapper.Normalize(contract);

        // Assert
        Assert.Equal(("Simple", "Diff"), (result?.DatabaseRecoveryModel, result?.BackupType));
    }

    [Fact]
    public void Normalize_WhenEnumNamesAreMissing_KeepsThemNull()
    {
        // Act
        StsDatabaseParametersDataModel? result =
            DatabaseParametersMapper.Normalize(new StsDatabaseParametersDataModel { DatabaseName = "Db" });

        // Assert
        Assert.Null(result?.DatabaseRecoveryModel);
        Assert.Null(result?.BackupType);
    }

    [Fact]
    public void FindUnknownEnumField_WhenContractIsMissing_ReturnsNull()
    {
        // Act
        string? result = DatabaseParametersMapper.FindUnknownEnumField(null);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Full", "TrLog")]
    [InlineData("simple", "diff")]
    public void FindUnknownEnumField_WhenEveryNameIsKnownOrMissing_ReturnsNull(string? recoveryModel,
        string? backupType)
    {
        // Arrange
        var contract = new StsDatabaseParametersDataModel
        {
            DatabaseRecoveryModel = recoveryModel, BackupType = backupType
        };

        // Act
        string? result = DatabaseParametersMapper.FindUnknownEnumField(contract);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Instant", null, "DatabaseRecoveryModel")]
    [InlineData("Instant", "Snapshot", "DatabaseRecoveryModel")]
    [InlineData(null, "Snapshot", "BackupType")]
    [InlineData("Full", "Snapshot", "BackupType")]
    public void FindUnknownEnumField_WhenANameIsUnknown_ReturnsItsField(string? recoveryModel, string? backupType,
        string expected)
    {
        // Arrange
        var contract = new StsDatabaseParametersDataModel
        {
            DatabaseRecoveryModel = recoveryModel, BackupType = backupType
        };

        // Act
        string? result = DatabaseParametersMapper.FindUnknownEnumField(contract);

        // Assert
        Assert.Equal(expected, result);
    }
}
