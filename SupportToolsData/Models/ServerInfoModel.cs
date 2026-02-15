using System.Collections.Generic;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

public sealed class ServerInfoModel : ItemData
{
    public string? ServerName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? WebAgentNameForCheck { get; set; }
    public int ServerSidePort { get; set; }
    public string? ApiVersionId { get; set; }
    public string? AppSettingsJsonSourceFileName { get; set; }

    public string? AppSettingsEncodedJsonFileName { get; set; }

    //public string? InstallScriptFileName { get; set; }
    public string? ServiceUserName { get; set; }

    public List<EProjectServerTools>? AllowToolsList { get; set; }

    //public DatabasesExchangeParameters? DatabasesExchangeParameters { get; set; }
    public DatabaseParameters? CurrentDatabaseParameters { get; init; }
    public DatabaseParameters? NewDatabaseParameters { get; init; }

    public override string GetItemKey()
    {
        return $"{ServerName}|{EnvironmentName}";
    }
}