using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportToolsData;

public class ProjectServerToolsFactoryStrategyParameters : IFactoryStrategyParameters
{
    public string ProjectName { get; set; }
    public ServerInfoModel ServerInfo { get; set; }
}
