using System.Collections.Generic;
using LibParameters;

namespace SupportToolsData.Models;

public sealed class ServerInfoModel : ItemData
{
    public string? WebAgentNameForCheck { get; set; }
    public int ServerSidePort { get; set; }
    public string? ApiVersionId { get; set; }
    public string? AppSettingsJsonSourceFileName { get; set; }
    public string? AppSettingsEncodedJsonFileName { get; set; }
    public string? ServiceUserName { get; set; }
    public List<ETools>? AllowToolsList { get; set; }
    public DatabasesExchangeParameters? DatabasesExchangeParameters { get; set; }
}