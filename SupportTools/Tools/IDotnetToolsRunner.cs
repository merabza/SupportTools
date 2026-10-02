using System.Collections.Generic;
using SystemTools.SharedKernel;

namespace SupportTools.Tools;

//dotnet tool-ის ბრძანებები, რომლებსაც DotnetToolsVersionsCheckerUpdater იყენებს. ტესტები მათ ცვლის, რომ
//ქსელი (dotnet tool search) და გლობალური ინსტალაცია არ დასჭირდეთ
internal interface IDotnetToolsRunner
{
    Result<IEnumerable<string>> GetToolsRawList();
    Result<(string, int)> SearchTool(string toolName);
    Result InstallTool(string packageId, string? version);
    Result UpdateTool(string packageId, string? version);
}
