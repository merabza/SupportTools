using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsEncoder;

public sealed class AppSettingsEncoderParameters : IParameters
{
    private AppSettingsEncoderParameters(string appSetEnKeysJsonFileName, string appSettingsEncodedJsonFileName,
        string keyPart1, string keyPart2)
    {
        AppSetEnKeysJsonFileName = appSetEnKeysJsonFileName;
        AppSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
        KeyPart1 = keyPart1;
        KeyPart2 = keyPart2;
    }

    public string AppSetEnKeysJsonFileName { get; }
    public string AppSettingsEncodedJsonFileName { get; }

    public string KeyPart1 { get; }
    public string KeyPart2 { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static AppSettingsEncoderParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        ////
        if (string.IsNullOrWhiteSpace(project.AppSetEnKeysJsonFileName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.KeyGuidPart))
        {
            StShared.WriteWarningLine(
                $"KeyGuidPart does not specified for project {projectName}, setting can not be encoded", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.AppSettingsJsonSourceFileName))
        {
            StShared.WriteWarningLine(
                $"AppSettingsJsonSourceFileName does not specified for server {serverInfo.GetItemKey()} and project {projectName}, setting can not be encoded",
                true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.AppSettingsEncodedJsonFileName))
        {
            StShared.WriteWarningLine(
                $"AppSettingsEncodedJsonFileName does not specified for server {serverInfo.GetItemKey()} and project {projectName}, setting can not be encoded",
                true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
        {
            StShared.WriteErrorLine("Server name is not specified", true);
            return null;
        }

        var appSettingsEncoderParameters = new AppSettingsEncoderParameters(project.AppSetEnKeysJsonFileName,
            serverInfo.AppSettingsEncodedJsonFileName, project.KeyGuidPart, serverInfo.ServerName);

        return appSettingsEncoderParameters;
    }
}
