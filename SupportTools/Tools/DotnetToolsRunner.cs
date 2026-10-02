using System.Collections.Generic;
using LibDotnetWork;
using SystemTools.SharedKernel;

namespace SupportTools.Tools;

internal sealed class DotnetToolsRunner : IDotnetToolsRunner
{
    private readonly DotnetProcessor _dotnetProcessor = new(null, false);

    public Result<IEnumerable<string>> GetToolsRawList()
    {
        return _dotnetProcessor.GetToolsRawList();
    }

    public Result<(string, int)> SearchTool(string toolName)
    {
        return _dotnetProcessor.SearchTool(toolName);
    }

    public Result InstallTool(string packageId, string? version)
    {
        return _dotnetProcessor.InstallTool(packageId, version);
    }

    public Result UpdateTool(string packageId, string? version)
    {
        return _dotnetProcessor.UpdateTool(packageId, version);
    }
}
