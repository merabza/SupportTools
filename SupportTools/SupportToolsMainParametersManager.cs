using Microsoft.Extensions.Options;
using ParametersManagement.LibParameters;

namespace SupportTools;

public class SupportToolsMainParametersManager : ParametersManager
{
    public SupportToolsMainParametersManager(IOptions<MainParametersManagerOptions> options) : base(options)
    {
    }
}
