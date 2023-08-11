using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class AppSettingsEncoderParameters : IParameters
{
    private AppSettingsEncoderParameters(string appSettingsJsonSourceFileName, string appSetEnKeysJsonFileName,
        string appSettingsEncodedJsonFileName, string keyPart1, string keyPart2, string projectName,
        ServerInfoModel serverInfo,
        string dateMask, string parametersFileExtension, FileStorageData fileStorageForExchange,
        SmartSchema exchangeSmartSchema)
    {
        AppSettingsJsonSourceFileName = appSettingsJsonSourceFileName;
        AppSetEnKeysJsonFileName = appSetEnKeysJsonFileName;
        AppSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
        KeyPart1 = keyPart1;
        KeyPart2 = keyPart2;
        ProjectName = projectName;
        ServerInfo = serverInfo;
        DateMask = dateMask;
        ParametersFileExtension = parametersFileExtension;
        FileStorageForExchange = fileStorageForExchange;
        ExchangeSmartSchema = exchangeSmartSchema;
    }

    public string AppSettingsJsonSourceFileName { get; }
    public string AppSetEnKeysJsonFileName { get; }
    public string AppSettingsEncodedJsonFileName { get; }
    public string KeyPart1 { get; }
    public string KeyPart2 { get; }
    public string ProjectName { get; }
    public ServerInfoModel ServerInfo { get; }
    public string DateMask { get; }
    public string ParametersFileExtension { get; }
    public FileStorageData FileStorageForExchange { get; }
    public SmartSchema ExchangeSmartSchema { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static AppSettingsEncoderParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var publisherDateMask = project.ParametersFileDateMask ?? supportToolsParameters.ParametersFileDateMask;
        if (string.IsNullOrWhiteSpace(publisherDateMask))
        {
            StShared.WriteErrorLine("PublisherDateMask does not specified in support tools parameters", true);
            return null;
        }

        var parametersFileExtension =
            project.ParametersFileExtension ?? supportToolsParameters.ParametersFileExtension;
        if (string.IsNullOrWhiteSpace(parametersFileExtension))
        {
            StShared.WriteErrorLine("parametersFileExtension does not specified in support tools parameters", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.AppSetEnKeysJsonFileName))
        {
            StShared.WriteErrorLine($"AppSetEnKeysJsonFileName does not specified for project {projectName}", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.KeyGuidPart))
        {
            StShared.WriteErrorLine($"KeyGuidPart does not specified for project {projectName}", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.AppSettingsJsonSourceFileName))
        {
            StShared.WriteErrorLine(
                $"AppSettingsJsonSourceFileName does not specified for server {serverInfo.GetItemKey()} and project {projectName}",
                true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.AppSettingsEncodedJsonFileName))
        {
            StShared.WriteErrorLine(
                $"AppSettingsEncodedJsonFileName does not specified for server {serverInfo.GetItemKey()} and project {projectName}",
                true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.SmartSchemaNameForExchange))
        {
            StShared.WriteErrorLine($"SmartSchemaNameForLocal does not specified for Project {projectName}", true);
            return null;
        }

        var smartSchemaForExchange =
            supportToolsParameters.GetSmartSchemaRequired(supportToolsParameters.SmartSchemaNameForExchange);

        if (string.IsNullOrWhiteSpace(supportToolsParameters.FileStorageNameForExchange))
        {
            StShared.WriteErrorLine($"FileStorageNameForExchange does not specified for Project {projectName}", true);
            return null;
        }

        var fileStorageForUpload =
            supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

        var appSettingsEncoderParameters = new AppSettingsEncoderParameters(serverInfo.AppSettingsJsonSourceFileName,
            project.AppSetEnKeysJsonFileName, serverInfo.AppSettingsEncodedJsonFileName, project.KeyGuidPart,
            serverInfo.ServerName, projectName, serverInfo, publisherDateMask, parametersFileExtension,
            fileStorageForUpload, smartSchemaForExchange);

        return appSettingsEncoderParameters;
    }
}