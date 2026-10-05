using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class GlobalSettingsMapperTests
{
    //the machine fields and the exchange's LocalPath stay on the local parameters, the shared fields come back
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalParameters_RestoresTheOriginal()
    {
        // Arrange
        SupportToolsParameters original = NewParameters();
        StsGlobalSettingsDataModel contract = GlobalSettingsMapper.ToContract(original);
        SupportToolsParameters restored = NewMachinePart();

        // Act
        GlobalSettingsMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ToContract_WhenCalled_LeavesMachineFieldsOut()
    {
        // Act
        StsGlobalSettingsDataModel result = GlobalSettingsMapper.ToContract(NewParameters());

        // Assert
        string json = MapperTestHelpers.JsonOf(result);
        Assert.DoesNotContain("SupportToolsServer", json);
        Assert.DoesNotContain(@"D:\\Local\\Backups", json);
        Assert.DoesNotContain(@"D:\\Logs", json);
        Assert.Equal("exchange", result.DatabasesBackupFilesExchange.ExchangeFileStorageName);
    }

    [Fact]
    public void ToContract_WhenExchangeParametersAreMissing_SendsAnEmptyPart()
    {
        // Act
        StsGlobalSettingsDataModel result = GlobalSettingsMapper.ToContract(new SupportToolsParameters());

        // Assert
        Assert.Null(result.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        Assert.Equal(MapperTestHelpers.HashOf(new StsGlobalSettingsDataModel()), MapperTestHelpers.HashOf(result));
    }

    [Fact]
    public void ApplyToLocal_WhenExchangePartIsEmptyAndLocalHasNone_CreatesNothing()
    {
        // Arrange
        var parameters = new SupportToolsParameters();

        // Act
        GlobalSettingsMapper.ApplyToLocal(new StsGlobalSettingsDataModel { ServiceDescriptionSignature = "x" },
            parameters);

        // Assert
        Assert.Equal("x", parameters.ServiceDescriptionSignature);
        Assert.Null(parameters.DatabasesBackupFilesExchangeParameters);
    }

    [Fact]
    public void ApplyToLocal_WhenExchangePartHasDataAndLocalHasNone_CreatesIt()
    {
        // Arrange
        var parameters = new SupportToolsParameters();
        var contract = new StsGlobalSettingsDataModel
        {
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                ExchangeFileStorageName = "exchange"
            }
        };

        // Act
        GlobalSettingsMapper.ApplyToLocal(contract, parameters);

        // Assert
        Assert.Equal("exchange", parameters.DatabasesBackupFilesExchangeParameters?.ExchangeFileStorageName);
        Assert.Null(parameters.DatabasesBackupFilesExchangeParameters?.LocalPath);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsGlobalSettingsDataModel
        {
            ServiceDescriptionSignature = "",
            UploadTempExtension = "",
            ProgramArchiveDateMask = "",
            ProgramArchiveExtension = "",
            ParametersFileDateMask = "",
            ParametersFileExtension = "",
            MediatRLicenseKey = "",
            FileStorageNameForExchange = "",
            SmartSchemaNameForExchange = "",
            SmartSchemaNameForLocal = "",
            LocalPackageManagerWebApiClientName = "",
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                DownloadTempExtension = "",
                UploadTempExtension = "",
                ExchangeFileStorageName = "",
                ExchangeSmartSchemaName = "",
                LocalSmartSchemaName = ""
            }
        };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(GlobalSettingsMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(GlobalSettingsMapper.Normalize(new StsGlobalSettingsDataModel()));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    private static SupportToolsParameters NewParameters()
    {
        SupportToolsParameters parameters = NewMachinePart();
        parameters.ServiceDescriptionSignature = "Fake signature";
        parameters.UploadTempExtension = ".up!";
        parameters.ProgramArchiveDateMask = "yyyyMMddHHmmss";
        parameters.ProgramArchiveExtension = ".zip";
        parameters.ParametersFileDateMask = "yyyyMMdd";
        parameters.ParametersFileExtension = ".json";
        parameters.MediatRLicenseKey = "fake-license-key";
        parameters.FileStorageNameForExchange = "exchange";
        parameters.SmartSchemaNameForExchange = "Daily";
        parameters.SmartSchemaNameForLocal = "Hourly";
        parameters.LocalPackageManagerWebApiClientName = "Packages";
        DatabasesBackupFilesExchangeParameters exchange = parameters.DatabasesBackupFilesExchangeParameters!;
        exchange.DownloadTempExtension = ".down!";
        exchange.UploadTempExtension = ".up!";
        exchange.ExchangeFileStorageName = "exchange";
        exchange.ExchangeSmartSchemaName = "Daily";
        exchange.LocalSmartSchemaName = "Hourly";
        return parameters;
    }

    //the fields of this computer only
    private static SupportToolsParameters NewMachinePart()
    {
        return new SupportToolsParameters
        {
            SupportToolsServerWebApiClientName = "SupportToolsServer",
            LogFolder = @"D:\Logs",
            WorkFolder = @"D:\Work",
            DatabasesBackupFilesExchangeParameters =
                new DatabasesBackupFilesExchangeParameters { LocalPath = @"D:\Local\Backups" }
        };
    }
}
